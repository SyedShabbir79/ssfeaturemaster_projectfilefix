using System.Text.Json.Serialization;

namespace FunctionsV2.Sys_StudentApp.Models;

public sealed class StudentAppResponse
{
    [JsonPropertyName("upn")]
    public string? Upn { get; init; }

    [JsonPropertyName("studentId")]
    public string? StudentId { get; init; }

    [JsonPropertyName("preferredName")]
    public string? PreferredName { get; init; }

    [JsonPropertyName("knownAsSurname")]
    public string? KnownAsSurname { get; init; }

    [JsonPropertyName("address")]
    public string? Address { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("photo")]
    public string? Photo { get; init; }

    [JsonPropertyName("courseCode")]
    public string? CourseCode { get; init; }

    [JsonPropertyName("courseName")]
    public string? CourseName { get; init; }

    [JsonPropertyName("courseBlock")]
    public string? CourseBlock { get; init; }

    [JsonPropertyName("department")]
    public string? Department { get; init; }

    [JsonPropertyName("faculty")]
    public string? Faculty { get; init; }

    [JsonPropertyName("studentType")]
    public string? StudentType { get; init; }

    public static StudentAppResponse Empty() => new()
    {
        Upn = string.Empty,
        StudentId = string.Empty,
        PreferredName = string.Empty,
        KnownAsSurname = string.Empty,
        Address = string.Empty,
        DateOfBirth = string.Empty,
        Photo = string.Empty,
        CourseCode = string.Empty,
        CourseName = string.Empty,
        CourseBlock = string.Empty,
        Department = string.Empty,
        Faculty = string.Empty,
        StudentType = string.Empty
    };
}
