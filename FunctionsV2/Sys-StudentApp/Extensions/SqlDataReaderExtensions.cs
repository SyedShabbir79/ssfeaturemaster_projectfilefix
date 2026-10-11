using Microsoft.Data.SqlClient;
using FunctionsV2.Sys_StudentApp.Models;
using System;

namespace FunctionsV2.Sys_StudentApp.Extensions;

internal static class SqlDataReaderExtensions
{
    public static StudentSqlRecord ReadStudentRecord(this SqlDataReader reader)
    {
        return new StudentSqlRecord(
            Upn: reader.GetNullableString("upn"),
            StudentId: reader.GetNullableString("studentId") ?? reader.GetNullableString("studentid"),
            PreferredName: reader.GetNullableString("preferredName"),
            KnownAsSurname: reader.GetNullableString("knownAsSurname"),
            Address: reader.GetNullableString("address"),
            DateOfBirth: reader.GetNullableDateTimeOffset("dateOfBirth"),
            Photo: reader.GetNullableString("photo"),
            CourseCode: reader.GetNullableString("courseCode"),
            CourseName: reader.GetNullableString("courseName"),
            CourseBlock: reader.GetNullableString("courseBlock"),
            Department: reader.GetNullableString("department"),
            Faculty: reader.GetNullableString("faculty"),
            StudentType: reader.GetNullableString("studentType"));
    }

    public static SqlStoredProcedureError ReadStoredProcedureError(this SqlDataReader reader)
    {
        return new SqlStoredProcedureError(
            ErrorNumber: reader.GetNullableInt32("ErrorNumber"),
            ErrorState: reader.GetNullableInt32("ErrorState"),
            ErrorMessage: reader.GetNullableString("ErrorMessage"));
    }

    private static string? GetNullableString(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.TryGetOrdinal(columnName);
        if (ordinal < 0 || reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            null => null,
            byte[] bytes => Convert.ToBase64String(bytes),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static int? GetNullableInt32(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.TryGetOrdinal(columnName);
        if (ordinal < 0 || reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            int i => i,
            long l => checked((int)l),
            short s => s,
            byte b => b,
            _ when int.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), out var parsed) => parsed,
            _ => null
        };
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.TryGetOrdinal(columnName);
        if (ordinal < 0 || reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dto => dto,
            DateTime dt => new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc)),
            string s when DateTimeOffset.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static int TryGetOrdinal(this SqlDataReader reader, string columnName)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
