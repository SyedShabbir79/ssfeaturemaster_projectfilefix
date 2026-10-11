using Azure.Core;
using FunctionsV2.Sys_StudentApp.Extensions;
using FunctionsV2.Sys_StudentApp.Models;
using FunctionsV2.Sys_StudentApp.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_StudentApp.Services;

public sealed class SqlStudentRepository : IStudentRepository
{
    private static readonly string[] SqlManagedIdentityScopes = { "https://database.windows.net/.default" };

    private readonly StudentAppOptions _options;
    private readonly string _connectionString;
    private readonly TokenCredential _tokenCredential;
    private readonly ILogger<SqlStudentRepository> _logger;

    public SqlStudentRepository(
        IOptions<StudentAppOptions> options,
        TokenCredential tokenCredential,
        IConfiguration configuration,
        ILogger<SqlStudentRepository> logger)
    {
        _options = options.Value;
        _connectionString = configuration["SqlConnectionString"]
            ?? configuration["Values:SqlConnectionString"]
            ?? string.Empty;
        _tokenCredential = tokenCredential;
        _logger = logger;
    }

    public async Task<StudentLookupResult> GetStudentAsync(string upn, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("SqlConnectionString app setting is missing.");
        }

        var maxAttempts = Math.Max(1, _options.SqlMaxRetryCount + 1);
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await ExecuteStoredProcedureAsync(upn, cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                lastException = ex;
                var delay = GetRetryDelay(attempt);
                _logger.LogWarning(
                    ex,
                    "Transient SQL error during student lookup. Attempt {Attempt}/{MaxAttempts}. Retrying after {DelayMs} ms.",
                    attempt,
                    maxAttempts,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException ex) when (attempt < maxAttempts)
            {
                lastException = ex;
                var delay = GetRetryDelay(attempt);
                _logger.LogWarning(
                    ex,
                    "SQL timeout during student lookup. Attempt {Attempt}/{MaxAttempts}. Retrying after {DelayMs} ms.",
                    attempt,
                    maxAttempts,
                    delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Student lookup failed without an exception being captured.");
    }

    private async Task<StudentLookupResult> ExecuteStoredProcedureAsync(string upn, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionString);

        if (_options.UseManagedIdentity)
        {
            var token = await _tokenCredential.GetTokenAsync(
                new TokenRequestContext(SqlManagedIdentityScopes),
                cancellationToken).ConfigureAwait(false);

            connection.AccessToken = token.Token;
        }

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var command = connection.CreateCommand();
        command.CommandText = _options.StoredProcedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = _options.SqlCommandTimeoutSeconds;

        var upnParameter = command.Parameters.Add("@upn", SqlDbType.NVarChar, 320);
        upnParameter.Value = upn;

        await using var reader = await command
            .ExecuteReaderAsync(CommandBehavior.SequentialAccess, cancellationToken)
            .ConfigureAwait(false);

        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return StudentLookupResult.FoundStudent(reader.ReadStudentRecord());
        }

        if (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false) &&
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var error = reader.ReadStoredProcedureError();
            if (error.ErrorNumber is not null || !string.IsNullOrWhiteSpace(error.ErrorMessage))
            {
                return StudentLookupResult.StoredProcedureError(error);
            }
        }

        return StudentLookupResult.NotFound();
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        var baseDelayMs = Math.Max(50, _options.SqlRetryBaseDelayMilliseconds);
        var exponentialDelay = baseDelayMs * Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.Next(0, baseDelayMs);
        return TimeSpan.FromMilliseconds(exponentialDelay + jitter);
    }

    private static bool IsTransient(SqlException exception)
    {
        foreach (SqlError error in exception.Errors)
        {
            if (IsTransientSqlErrorNumber(error.Number))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTransientSqlErrorNumber(int number)
    {
        return number is
            4060 or     // Cannot open database requested by the login.
            40197 or    // Service encountered an error processing request.
            40501 or    // Service busy / throttling.
            40613 or    // Database unavailable.
            49918 or    // Not enough resources.
            49919 or
            49920 or
            10928 or    // Resource limit reached.
            10929 or
            11001 or    // Connection failure.
            10053 or
            10054 or
            10060;
    }
}

