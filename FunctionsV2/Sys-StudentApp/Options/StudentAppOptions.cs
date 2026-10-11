namespace FunctionsV2.Sys_StudentApp.Options;

public sealed class StudentAppOptions
{
    public const string SectionName = "StudentApp";

    public string StoredProcedureName { get; set; } = "[dbo].[UoR_AIS_StudentApp]";

    public bool UseManagedIdentity { get; set; } = false;

    public string? ManagedIdentityClientId { get; set; }

    public int SqlCommandTimeoutSeconds { get; set; } = 30;

    public int SqlMaxRetryCount { get; set; } = 3;

    public int SqlRetryBaseDelayMilliseconds { get; set; } = 250;

    public bool Return404WhenStudentNotFound { get; set; } = false;

    public string DefaultPhotoMimeType { get; set; } = "image/jpeg";
}