//using Azure.Core;
//using Azure.Identity;
//using Microsoft.Azure.Documents;
//using Microsoft.Data.SqlClient;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using System.Data;
//using System.Threading;
//using System.Threading.Tasks;

//namespace FunctionsV2.Helpers
//{
//    public interface ISqlDbHelper
//    {
//        //Connects only to UoR Cache database, executes the query and returns the result as a DataTable.
//        Task<DataTable> ExecuteQueryAsync(string sql, params SqlParameter[]? parameters);
//        Task<int> ExecuteNonQueryAsync(string sql, params SqlParameter[]? parameters);
//        Task<object?> ExecuteScalarAsync(string sql, params SqlParameter[]? parameters);
//    }

//    public class SqlDbHelper : ISqlDbHelper
//    {
//        private readonly IConfiguration _config;
//        private readonly ILogger<SqlDbHelper> _logger;
//        private readonly TokenCredential _tokenCredential;
//        private static readonly string[] SqlManagedIdentityScopes = new[] { "https://database.windows.net/.default" };

//        public SqlDbHelper(IConfiguration configuration, ILogger<SqlDbHelper> logger)
//        {
//            _config = configuration;
//            _logger = logger;
//            _tokenCredential = new DefaultAzureCredential();
//        }

//        public async Task<DataTable> ExecuteQueryAsync(
//            string sql,
//            params SqlParameter[]? parameters)
//        {
//            var table = new DataTable();

//            await using var connection = new SqlConnection(_config["SqlConnectionString"]);

//#if !DEBUG
//            var token = await _tokenCredential.GetTokenAsync(
//                new TokenRequestContext(SqlManagedIdentityScopes),
//                CancellationToken.None).ConfigureAwait(false);

//            connection.AccessToken = token.Token;
//#endif

//            await connection.OpenAsync().ConfigureAwait(false);

//            await using var command = new SqlCommand(sql, connection);

//            if (parameters != null && parameters.Length > 0)
//            {
//                command.Parameters.AddRange(parameters);
//            }

//            await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

//            table.Load(reader);

//            return table;
//        }

//        public async Task<int> ExecuteNonQueryAsync(
//            string sql,
//            params SqlParameter[]? parameters)
//        {
//            await using var connection = new SqlConnection(_config["SqlConnectionString"]);

//#if !DEBUG
//            var token = await _tokenCredential.GetTokenAsync(
//                new TokenRequestContext(SqlManagedIdentityScopes),
//                CancellationToken.None).ConfigureAwait(false);

//            connection.AccessToken = token.Token;
//#endif

//            await connection.OpenAsync().ConfigureAwait(false);

//            await using var command = new SqlCommand(sql, connection);

//            if (parameters != null && parameters.Length > 0)
//            {
//                command.Parameters.AddRange(parameters);
//            }

//            return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
//        }

//        public async Task<object?> ExecuteScalarAsync(
//            string sql,
//            params SqlParameter[]? parameters)
//        {
//            await using var connection = new SqlConnection(_config["SqlConnectionString"]);

//#if !DEBUG
//            var token = await _tokenCredential.GetTokenAsync(
//                new TokenRequestContext(SqlManagedIdentityScopes),
//                CancellationToken.None).ConfigureAwait(false);

//            connection.AccessToken = token.Token;
//#endif

//            await connection.OpenAsync().ConfigureAwait(false);

//            await using var command = new SqlCommand(sql, connection);

//            if (parameters != null && parameters.Length > 0)
//            {
//                command.Parameters.AddRange(parameters);
//            }

//            return await command.ExecuteScalarAsync().ConfigureAwait(false);
//        }
//    }
//}
