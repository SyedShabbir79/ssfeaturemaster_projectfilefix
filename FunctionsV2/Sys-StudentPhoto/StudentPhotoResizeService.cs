using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_StudentPhoto.Services
{
    public class StudentPhotoResizeService : IStudentPhotoResizeService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<StudentPhotoResizeService> _logger;

        public StudentPhotoResizeService(
            IConfiguration config,
            ILogger<StudentPhotoResizeService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<ResizeResult> ResizeAndUpsertAsync(string stuCode, string photoPayload, string photoFileName)
        {
            _logger.LogInformation("ResizeAndUpsertAsync started for StuCode={StuCode}", stuCode);

            var inputBytes = PhotoPayloadParser.ParseToImageBytes(photoPayload);
            var resized = ImageResizeHelper.ResizeJpeg(inputBytes, 160, 65);

            var result = await UpsertAsync(stuCode, resized, photoFileName);

            _logger.LogInformation(
                "ResizeAndUpsertAsync completed for StuCode={StuCode}, Operation={Operation}, ResizedBytes={ResizedBytes}",
                result.StuCode, result.Operation, result.ResizedBytes);

            return result;
        }

        public async Task<ResizeResult> ResizeAndUpsertFromDbPhotoAsync(string stuCode, byte[] dbPhoto, string photoFileName)
        {
            _logger.LogInformation("ResizeAndUpsertFromDbPhotoAsync started for StuCode={StuCode}", stuCode);

            var normalized = NormalizeDbBytes(dbPhoto);
            var resized = ImageResizeHelper.ResizeJpeg(normalized, 160, 65);

            var result = await UpsertAsync(stuCode, resized, photoFileName);

            _logger.LogInformation(
                "ResizeAndUpsertFromDbPhotoAsync completed for StuCode={StuCode}, Operation={Operation}, ResizedBytes={ResizedBytes}",
                result.StuCode, result.Operation, result.ResizedBytes);

            return result;
        }

        public async Task<IReadOnlyList<(string StuCode, byte[] Photo, string PhotoFileName)>> GetUnresizedBatchAsync(int top)
        {
            _logger.LogInformation("Fetching unresized batch. Top={Top}", top);

            var list = new List<(string, byte[], string)>();

            await using var conn = new SqlConnection(_config["SqlConnectionString"]);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"SELECT TOP (@top) stu_code, photo, photo_filename FROM dbo.ais_students WHERE photo IS NOT NULL AND ISNULL(photo_resized, 0) = 0 ORDER BY stu_code;";
            cmd.Parameters.AddWithValue("@top", top);

            await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.Default);
            while (await rdr.ReadAsync())
            {
                list.Add((
                    rdr.GetString(0),
                    (byte[])rdr["photo"],
                    rdr.IsDBNull(2) ? null : rdr.GetString(2)
                ));
            }

            _logger.LogInformation("Fetched {Count} unresized rows", list.Count);
            return list;
        }

        private async Task<ResizeResult> UpsertAsync(string stuCode, byte[] resized, string photoFileName)
        {
            await using var conn = new SqlConnection(_config["SqlConnectionString"]);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("dbo.UoR_AIS_UpsertStudentPhotoResized", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@StuCode", stuCode);
            cmd.Parameters.Add("@Photo", SqlDbType.VarBinary, -1).Value = resized;
            cmd.Parameters.AddWithValue("@PhotoFileName", string.IsNullOrWhiteSpace(photoFileName) ? $"{stuCode}.jpg" : photoFileName);

            var wasUpdateParam = new SqlParameter("@WasUpdate", SqlDbType.Bit) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(wasUpdateParam);

            await cmd.ExecuteNonQueryAsync();

            var wasUpdate = wasUpdateParam.Value != DBNull.Value && (bool)wasUpdateParam.Value;
            return new ResizeResult(stuCode, wasUpdate ? "UPDATE" : "INSERT", resized.Length);
        }

        private static byte[] NormalizeDbBytes(byte[] source)
        {
            if (source == null || source.Length == 0)
                throw new InvalidOperationException("Photo is empty.");

            if (source.Length >= 3 && source[0] == 0xFF && source[1] == 0xD8 && source[2] == 0xFF)
                return source;

            var text = Encoding.UTF8.GetString(source).Trim();
            try { return Convert.FromBase64String(text); } catch { return source; }
        }
    }    
}