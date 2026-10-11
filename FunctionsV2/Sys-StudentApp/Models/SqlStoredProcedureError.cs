namespace FunctionsV2.Sys_StudentApp.Models;

public sealed record SqlStoredProcedureError(
    int? ErrorNumber,
    int? ErrorState,
    string? ErrorMessage);
