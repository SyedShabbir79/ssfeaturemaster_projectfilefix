using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionsV2.Helpers
{
    public interface IServiceBusHelper
    {
        Task<ServiceBusReceivedMessage?> GetMessageAsync(string entityName, string? subscriptionName = null, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceBusReceivedMessage>> GetMessagesAsync(string entityName, string? subscriptionName = null, int maxMessages = 10, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default);

        Task<ServiceBusReceivedMessage> CompleteMessageAsync(ServiceBusReceivedMessage message, string entityName, string? subscriptionName = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceBusReceivedMessage>> CompleteMessagesAsync(IEnumerable<ServiceBusReceivedMessage> messages, string entityName, string? subscriptionName = null, CancellationToken cancellationToken = default);

        Task<ServiceBusReceivedMessage> MoveMessageToDeadLetterAsync(ServiceBusReceivedMessage message, string entityName, string? subscriptionName = null, string? deadLetterReason = null, string? deadLetterErrorDescription = null, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ServiceBusReceivedMessage>> MoveMessagesToDeadLetterAsync(IEnumerable<ServiceBusReceivedMessage> messages, string entityName, string? subscriptionName = null, string? deadLetterReason = null, string? deadLetterErrorDescription = null, CancellationToken cancellationToken = default);

        Task<ServiceBusReceivedMessage> AbandonMessageAsync(ServiceBusReceivedMessage message, string entityName, string? subscriptionName = null, IDictionary<string, object>? propertiesToModify = null, CancellationToken cancellationToken = default);
    }

    public class ServiceBusHelper : IServiceBusHelper
    {
        private readonly IConfiguration _config;
        private readonly ServiceBusClient _serviceBusClient;

        public ServiceBusHelper(IConfiguration config)
        {
            _config = config;
            _serviceBusClient = new ServiceBusClient(_config["SB1ConnectionString"]);
        }

        private ServiceBusReceiver CreateReceiver(string entityName, string? subscriptionName = null)
        {
            if (string.IsNullOrWhiteSpace(subscriptionName))
                return _serviceBusClient.CreateReceiver(entityName);
            return _serviceBusClient.CreateReceiver(entityName, subscriptionName);
        }

        public async Task<ServiceBusReceivedMessage?> GetMessageAsync(string entityName, string? subscriptionName = null, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            await using var receiver = CreateReceiver(entityName, subscriptionName);
            var msg = await receiver.ReceiveMessageAsync(maxWaitTime, cancellationToken).ConfigureAwait(false);
            return msg;
        }

        public async Task<IReadOnlyList<ServiceBusReceivedMessage>> GetMessagesAsync(string entityName, string? subscriptionName = null, int maxMessages = 10, TimeSpan? maxWaitTime = null, CancellationToken cancellationToken = default)
        {
            await using var receiver = CreateReceiver(entityName, subscriptionName);
            var msgs = await receiver.ReceiveMessagesAsync(maxMessages, maxWaitTime, cancellationToken).ConfigureAwait(false);
            return msgs;
        }

        public async Task<ServiceBusReceivedMessage> CompleteMessageAsync(ServiceBusReceivedMessage message, string entityName, string? subscriptionName = null, CancellationToken cancellationToken = default)
        {
            await using var receiver = CreateReceiver(entityName, subscriptionName);
            await receiver.CompleteMessageAsync(message, cancellationToken).ConfigureAwait(false);
            return message;
        }

        public async Task<IReadOnlyList<ServiceBusReceivedMessage>> CompleteMessagesAsync(IEnumerable<ServiceBusReceivedMessage> messages, string entityName, string? subscriptionName = null, CancellationToken cancellationToken = default)
        {
            await using var receiver = CreateReceiver(entityName, subscriptionName);
            var completed = new List<ServiceBusReceivedMessage>();
            foreach (var msg in messages)
            {
                await receiver.CompleteMessageAsync(msg, cancellationToken).ConfigureAwait(false);
                completed.Add(msg);
            }
            return completed;
        }

        public async Task<ServiceBusReceivedMessage> MoveMessageToDeadLetterAsync(ServiceBusReceivedMessage message, string entityName, string? subscriptionName = null, string? deadLetterReason = null, string? deadLetterErrorDescription = null, CancellationToken cancellationToken = default)
        {
            await using var receiver = CreateReceiver(entityName, subscriptionName);
            await receiver.DeadLetterMessageAsync(message, deadLetterReason, deadLetterErrorDescription, cancellationToken).ConfigureAwait(false);
            return message;
        }

        public async Task<IReadOnlyList<ServiceBusReceivedMessage>> MoveMessagesToDeadLetterAsync(IEnumerable<ServiceBusReceivedMessage> messages, string entityName, string? subscriptionName = null, string? deadLetterReason = null, string? deadLetterErrorDescription = null, CancellationToken cancellationToken = default)
        {
            await using var receiver = CreateReceiver(entityName, subscriptionName);
            var deaded = new List<ServiceBusReceivedMessage>();
            foreach (var msg in messages)
            {
                await receiver.DeadLetterMessageAsync(msg, deadLetterReason, deadLetterErrorDescription, cancellationToken).ConfigureAwait(false);
                deaded.Add(msg);
            }
            return deaded;
        }

        public async Task<ServiceBusReceivedMessage> AbandonMessageAsync(ServiceBusReceivedMessage message, string entityName, string? subscriptionName = null, IDictionary<string, object>? propertiesToModify = null, CancellationToken cancellationToken = default)
        {
            await using var receiver = CreateReceiver(entityName, subscriptionName);
            await receiver.AbandonMessageAsync(message, propertiesToModify, cancellationToken).ConfigureAwait(false);
            return message;
        }
    }
}
