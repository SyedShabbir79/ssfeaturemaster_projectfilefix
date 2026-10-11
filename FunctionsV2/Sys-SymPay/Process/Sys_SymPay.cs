using FunctionsV2.Helpers;
using FunctionsV2.Sys_SymPay.Exceptions;
using FunctionsV2.Sys_SymPay.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_SymPay;

public class Sys_SymPay
{
    private readonly ISymPayHelper _symPayHelper;
    private readonly IServiceBusHelper _serviceBusHelper;
    private readonly IConfiguration _configuration;
    private readonly ITableStorageService _tableStorageService;
    //private readonly ISqlDbHelper _sqlDbHelper;

    // Global/current MAC stored in-memory for the lifetime of this class instance
    // This prevents losing progress if processing breaks midway within a single host instance.
    // Thread-safety: access should be guarded by _macLock when being updated.
    private long _currentMac = 0;
    private long _accountId = 0;
    private readonly object _macLock = new();

    //Constants
    private const string _tableName = "UoRConfig";
    private const string _tablePartitionKey = "Sys.SymPay.Cards";
    private const string _tableCurrentMacRowKey = "CurrentMac";
    private const string _tableResetMacFlagRowKey = "ResetMacFlag";
    private const string _serviceBusQueueName = "sys.cards.sympay";

    public Sys_SymPay(
        ISymPayHelper symPayHelper,
        ILogger<Sys_SymPay> logger,
        IServiceBusHelper serviceBusHelper,
        //ISqlDbHelper sqlDbHelper,
        ITableStorageService tableStorage,
        IConfiguration configuration)
    {
        _symPayHelper = symPayHelper;
        _serviceBusHelper = serviceBusHelper;
        //_sqlDbHelper = sqlDbHelper;
        _tableStorageService = tableStorage;
        _configuration = configuration;
    }

    [FunctionName("Sys_SymPay_ProcessUpdate")]
    public async Task<IActionResult> Run(
        [HttpTrigger(
            AuthorizationLevel.Function,
            "post",
            Route = null)]
        HttpRequest req,
        ILogger log,
        CancellationToken cancellationToken)
    {
        try
        {
            log.LogInformation("Starting SymPay batch processing.");

            var settings =
                await _tableStorageService.QueryTableStorageByFilter<UoRConfigTableEntity>(
                    _tableName,
                    $"PartitionKey eq '{_tablePartitionKey}'");

            var batchsize = settings.FirstOrDefault(x => x.RowKey == "BatchSize")?.intValue1 ?? 30;
            log.LogInformation("Batch size from config: {BatchSize}", batchsize);

            var messages =
                await _serviceBusHelper.GetMessagesAsync(
                    _serviceBusQueueName,
                    null,
                    (int)batchsize);

            log.LogInformation("Retrieved {MessageCount} message(s) from queue {QueueName}.", messages.Count, _serviceBusQueueName);

            if (messages.Count == 0)
            {
                log.LogInformation("No SymPay messages to process.");
                return new OkObjectResult(new List<SymPayProcessUpdateResponse>());
            }

            // Initialize _currentMac from table storage if not already set or if reset flag is true
            var storedMac = settings.FirstOrDefault(x => x.RowKey == _tableCurrentMacRowKey)?.intValue1 ?? 0;
            var resetFlag = settings.FirstOrDefault(x => x.RowKey == _tableResetMacFlagRowKey)?.boolValue == true;

            lock (_macLock)
            {
                if (_currentMac == 0)
                {
                    _currentMac = storedMac;
                }
            }

            if (_currentMac == 0 || resetFlag)
            {
                var fetchedMac = await _symPayHelper.ResetCurrentMacAsync();
                LockCurrentMac(fetchedMac);

                log.LogInformation("Fetched current MAC from SymPay: {CurrentMac}", _currentMac);
            }
            else
            {
                log.LogInformation("Using stored current MAC: {CurrentMac}", _currentMac);
            }

            var processed = 0;
            var responses = new List<SymPayProcessUpdateResponse>();

            // IMPORTANT: sequential - no Task.WhenAll/Parallel.ForEach
            foreach (var message in messages)
            {
                CampusCard card = null!;

                try
                {
                    card = (JsonSerializer.Deserialize<CampusCard>(message.Body)!);

                    log.LogInformation(
                        "Begin processing card. EmosCode: {EmosCode}, MessageId: {MessageId}, DeliveryCount: {DeliveryCount}, CurrentMac: {CurrentMac}",
                        card?.EmosCode ?? "unknown",
                        message.MessageId,
                        message.DeliveryCount,
                        _currentMac);

                    if (card is null)
                        throw new InvalidOperationException(
                            "Unable to deserialize card update.");

                    // ProcessUpdateAsync returns the new currentMac and accountId.
                    _accountId = await ProcessUpdateAsync(
                        card,
                        cancellationToken);

                    log.LogInformation(
                        "Finished processing card. EmosCode: {EmosCode}, NewMac: {NewMac}, SymPayAccountId: {SymPayAccountId}",
                        card.EmosCode,
                        _currentMac,
                        _accountId);

                    // Update the SymPay ID in the cards database only if the accountId is valid (non-null and non-zero)
                    if (_accountId != 0)
                    {
                        // Collect response for this iteration
                        // Ensure any existing entry with the same emos_code is removed, then add the new one
                        responses.RemoveAll(r => r.emos_code == card.EmosCode);

                        responses.Add(new SymPayProcessUpdateResponse
                        {
                            sympay_id = _accountId,
                            emos_code = card.EmosCode
                        });

                        log.LogInformation(
                            "Will update card DB: set SymPayId {SymPayId} for card EmosCode {EmosCode}",
                            _accountId,
                            card.EmosCode);
                    }
                    else
                    {
                        log.LogInformation(
                            "No SymPay account id returned for card EmosCode {EmosCode}. Nothing to update in card DB.",
                            card.EmosCode);
                    }

                    await _serviceBusHelper.CompleteMessageAsync(
                        message,
                        _serviceBusQueueName);

                    log.LogInformation(
                        "Completed message {MessageId} for card EmosCode {EmosCode}.",
                        message.MessageId,
                        card.EmosCode);

                    processed++;
                }
                catch (SymPayBusinessException ex)
                {
                    log.LogWarning(
                        ex,
                        "SymPay business failure for message {MessageId}, EmosCode: {EmosCode}",
                        message.MessageId,
                        card?.EmosCode ?? "unknown");

                    log.LogInformation("Updating stored MAC to {CurrentMac} due to business failure.", _currentMac);
                    await UpdateCurrentMac(_currentMac);

                    await _serviceBusHelper.MoveMessageToDeadLetterAsync(
                        message,
                        _serviceBusQueueName,
                        deadLetterErrorDescription: "SymPay business failure",
                        deadLetterReason: ex.Message);

                    log.LogWarning(
                        "Moved message {MessageId} (EmosCode: {EmosCode}) to dead letter queue.",
                        message.MessageId,
                        card?.EmosCode ?? "unknown");
                }
                catch (SymPayRetryableException ex)
                {
                    log.LogWarning(
                        ex,
                        "Retryable SymPay failure for message {MessageId}, EmosCode: {EmosCode}. Abandoning and stopping batch.",
                        message.MessageId,
                        card?.EmosCode ?? "unknown");

                    log.LogInformation("Updating stored MAC to {CurrentMac} due to retryable failure.", _currentMac);
                    await UpdateCurrentMac(_currentMac);

                    await _serviceBusHelper.AbandonMessageAsync(
                        message,
                        _serviceBusQueueName);

                    log.LogInformation(
                        "Abandoned message {MessageId} (EmosCode: {EmosCode}). Stopping batch to preserve sequential MAC processing.",
                        message.MessageId,
                        card?.EmosCode ?? "unknown");

                    // Stop the batch because MAC processing is sequential.
                    break;
                }
            }

            log.LogInformation("Updating stored MAC to {CurrentMac} after batch processing.", _currentMac);
            await UpdateCurrentMac(_currentMac);

            log.LogInformation("Batch processing complete. Processed {ProcessedCount} messages.", processed);
            return new OkObjectResult(responses);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "SymPay batch processing failed. Updating stored MAC to {CurrentMac}.", _currentMac);
            await UpdateCurrentMac(_currentMac);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task UpdateCurrentMac(long currentMac)
    {
        await _tableStorageService.UpsertEntityAsync(
            _tableName,
            new UoRConfigTableEntity
            {
                PartitionKey = _tablePartitionKey,
                RowKey = _tableCurrentMacRowKey,
                intValue1 = currentMac
            });
    }

    private async Task<long> ProcessUpdateAsync(
        CampusCard card,
        CancellationToken cancellationToken)
    {
        // Generate email address for SymPay account lookup based on card data.
        var email = GenerateEmail(card);

        if (string.IsNullOrEmpty(email))
            throw new SymPayBusinessException(
                "No email address available for SymPay account lookup.");
        /*
         * STEP 1
         * Find existing SymPay account.
         */
        var accountLookup =
            await _symPayHelper.GetAccountIdAsync(
                email,
                _currentMac,
                cancellationToken);

        LockCurrentMac(accountLookup.NextMacCode);

        var accountId = accountLookup.AccountId;

        // If no account found, try looking up by EmosCode email.This is a fallback in case the primary email lookup fails (ESB logic)
        if (accountId <= 0)
        {
            var emosCodeEmail = card.EmosCode + "@reading.ac.uk";

            var accountLookupForEmosCodeEmail =
                await _symPayHelper.GetAccountIdAsync(
                    emosCodeEmail,
                    _currentMac,
                    cancellationToken);

            accountId = accountLookupForEmosCodeEmail.AccountId;

            LockCurrentMac(accountLookupForEmosCodeEmail.NextMacCode);
        }

        /*
         * STEP 2
         * Account doesn't exist -> create.
         */
        if (accountId <= 0)
        {
            var createResult =
                await _symPayHelper.ImportNewAccountAsync(
                    card,
                    email,
                    _currentMac,
                    cancellationToken);

            if (createResult.AccountId <= 0)
                throw new SymPayBusinessException(
                    "SymPay account creation did not return an Account ID.");

            return createResult.AccountId;
        }

        var updateResult =
            await _symPayHelper.ImportExistingAccountAsync(
                card,
                email,
                _currentMac,
                accountId,
                cancellationToken);

        /*
         * STEP 4
         * Retrieve current SymPay cards.
         */
        var cardsResult =
            await _symPayHelper.GetCardsForAccountAsync(
                accountId,
                _currentMac,
                cancellationToken);

        LockCurrentMac(cardsResult.NextMacCode);

        /*
         * STEP 5
         * Compare current Campus Cards MAG against SymPay.
         *
         * This replaces old_emos_mag / bts_changes.
         */
        var actions = DetermineCardActions(
            accountId,
            email,
            card.EmosMag,
            cardsResult.Cards);

        if (actions.Count == 0)
            return accountId;

        /*
         * STEP 6
         * Build model -> helper serializes it to SOAP.
         */
        var cardRequest =
            BuildUpdateCardsRequest(
                actions,
                _currentMac);

        await _symPayHelper.UpdateCardsAsync(
            cardRequest,
            cancellationToken);

        return accountId;
    }

    private void LockCurrentMac(long NextMacCode)
    {
        lock (_macLock)
        {
            _currentMac = NextMacCode;
        }
    }

    private static List<UpdateCard> DetermineCardActions(
        long accountId,
        string email,
        string? incomingMag,
        IReadOnlyCollection<SymPayCard> currentCards)
    {
        var actions = new List<UpdateCard>();

        /*
         * Campus Cards has no current MAG.
         *
         * We no longer have old_emos_mag, therefore current active
         * SymPay cards are treated as the previous card(s).
         */
        if (string.IsNullOrWhiteSpace(incomingMag))
        {
            var toProcess = currentCards.Where(x => x.Active && !x.Lost).ToList();

            foreach (var card in toProcess)
            {
                actions.Add(CreateCardAction(
                    accountId,
                    email,
                    card.CardId,
                    CardUpdateAction.MarkLost));

                actions.Add(CreateCardAction(
                    accountId,
                    email,
                    card.CardId,
                    CardUpdateAction.MarkNotActive));
            }

            return actions;
        }

        /*
         * Check ALL cards, not only active cards.
         *
         * This follows legacy GenerateNewCard behaviour.
         */
        var incomingCard =
            currentCards.FirstOrDefault(x =>
                string.Equals(
                    x.CardId,
                    incomingMag,
                    StringComparison.OrdinalIgnoreCase));

        var otherActiveCards =
            currentCards
                .Where(x =>
                    x.Active &&
                    !string.Equals(
                        x.CardId,
                        incomingMag,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

        /*
         * Card doesn't exist.
         *
         * No previous active card -> New Card.
         * Existing active card -> Replacement.
         */
        if (incomingCard is null)
        {
            foreach (var oldCard in otherActiveCards)
            {
                actions.Add(CreateCardAction(
                    accountId,
                    email,
                    oldCard.CardId,
                    CardUpdateAction.MarkNotActive));
            }

            actions.Add(CreateCardAction(
                accountId,
                email,
                incomingMag,
                CardUpdateAction.Add));

            return actions;
        }

        /*
         * Card already exists and is current.
         * Nothing required.
         */
        if (incomingCard.Active && !incomingCard.Lost)
        {
            return actions;
        }

        /*
         * Incoming card already exists but another card is currently active.
         *
         * Legacy replacement behaviour would not Add the existing
         * card again, but would make the previous active card inactive.
         */

        foreach (var oldCard in otherActiveCards)
        {
            actions.Add(CreateCardAction(
                accountId,
                email,
                oldCard.CardId,
                CardUpdateAction.MarkNotActive));
        }

        return actions;
    }

    private UpdateCards BuildUpdateCardsRequest(
        List<UpdateCard> actions,
        long mac)
    {
        return new UpdateCards
        {
            ImportDetail = new ImportDetailUpdateCards
            {
                ID1 = GetRequiredSetting("SymPayDatasource"),
                ID2 = GetRequiredSetting("SymPayDatabase"),

                ConnectorID =
                    int.Parse(GetRequiredSetting("SymPayConnectorID")),

                MACCode = mac,
                LookupMethod = LookupMethod.AccountID,

                UpdateCardList = actions
            }
        };
    }

    private static UpdateCard CreateCardAction(
        long accountId,
        string email,
        string cardId,
        CardUpdateAction action)
    {
        return new UpdateCard
        {
            AccountID = accountId,
            Email = email,
            CardID = long.Parse(cardId),
            Action = action
        };
    }

    private string GetRequiredSetting(string key) =>
        _configuration[key]
        ?? throw new InvalidOperationException(
            $"Missing configuration: {key}");

    private static string GenerateEmail(CampusCard card)
    {
        if (card == null) throw new ArgumentNullException(nameof(card));

        bool isStudent = string.Equals(card.EmosType, EmosType.STUDENT.ToString(), StringComparison.OrdinalIgnoreCase);
        string? enrichedUsername = card.EnrichedData?.Username;
        string? enrichedUpn = card.EnrichedData?.Upn;
        string? emosUpn = card.EmosUpn;

        if (isStudent)
        {
            if (!string.IsNullOrEmpty(enrichedUsername))
                return $"{enrichedUsername}@reading.ac.uk";

            if (!string.IsNullOrEmpty(enrichedUpn))
            {
                var local = enrichedUpn.Split('@')[0];
                if (!string.IsNullOrEmpty(local))
                    return $"{local}@reading.ac.uk";
            }

            if (!string.IsNullOrEmpty(emosUpn))
            {
                var local = emosUpn.Split('@')[0];
                if (!string.IsNullOrEmpty(local))
                    return $"{local}@reading.ac.uk";
            }

            throw new Exception("Invalid Email Address Generation for card with EmosCode: " + card.EmosCode);
        }
        else
        {
            if (!string.IsNullOrEmpty(enrichedUpn))
                return enrichedUpn;

            if (!string.IsNullOrEmpty(emosUpn))
                return emosUpn;

            throw new Exception("Invalid Email Address Generation for card with EmosCode: " + card.EmosCode);
        }
    }
}