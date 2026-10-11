using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FunctionsV2.Sys_StudentApp.Models;
using FunctionsV2.Sys_StudentApp.Options;
using System;

namespace FunctionsV2.Sys_StudentApp.Services;

public sealed class StudentResponseMapper : IStudentResponseMapper
{
    private readonly StudentAppOptions _options;

    public StudentResponseMapper(IOptions<StudentAppOptions> options)
    {
        _options = options.Value;
    }

    public StudentAppResponse Map(StudentSqlRecord record)
    {
        return new StudentAppResponse
        {
            Upn = record.Upn,
            StudentId = record.StudentId,
            PreferredName = record.PreferredName,
            KnownAsSurname = record.KnownAsSurname,
            Address = record.Address,
            DateOfBirth = record.DateOfBirth?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Photo = MapPhoto(record.Photo),
            CourseCode = record.CourseCode,
            CourseName = record.CourseName,
            CourseBlock = record.CourseBlock,
            Department = record.Department,
            Faculty = record.Faculty,
            StudentType = record.StudentType
        };
    }

    private string? MapPhoto(string? photo)
    {
        if (string.IsNullOrWhiteSpace(photo))
        {
            return null;
        }

        var trimmed = photo.Trim();

        if (trimmed.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        try
        {
            using var json = JsonDocument.Parse(trimmed);
            var root = json.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("value", out var valueElement) &&
                valueElement.ValueKind == JsonValueKind.String)
            {
                var base64Value = valueElement.GetString();
                if (string.IsNullOrWhiteSpace(base64Value))
                {
                    return null;
                }

                var mimeType = _options.DefaultPhotoMimeType;

                if (root.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String)
                {
                    mimeType = NormaliseMimeType(typeElement.GetString(), _options.DefaultPhotoMimeType);
                }

                return $"data:{mimeType};base64,{base64Value}";
            }
        }
        catch (JsonException)
        {
            // The stored procedure may return the raw base64 string rather than a { value, type } object.
        }

        return $"data:{_options.DefaultPhotoMimeType};base64,{trimmed}";
    }

    private static string NormaliseMimeType(string? imageType, string defaultMimeType)
    {
        if (string.IsNullOrWhiteSpace(imageType))
        {
            return defaultMimeType;
        }

        var type = imageType.Trim().ToLowerInvariant();

        if (type.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return type;
        }

        return type switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "gif" => "image/gif",
            "webp" => "image/webp",
            _ => defaultMimeType
        };
    }
}
