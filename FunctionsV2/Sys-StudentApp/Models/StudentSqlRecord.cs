using System;

namespace FunctionsV2.Sys_StudentApp.Models;

public sealed record StudentSqlRecord(
    string? Upn,
    string? StudentId,
    string? PreferredName,
    string? KnownAsSurname,
    string? Address,
    DateTimeOffset? DateOfBirth,
    string? Photo,
    string? CourseCode,
    string? CourseName,
    string? CourseBlock,
    string? Department,
    string? Faculty,
    string? StudentType);
