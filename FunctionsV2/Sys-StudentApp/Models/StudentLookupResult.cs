namespace FunctionsV2.Sys_StudentApp.Models;

public sealed class StudentLookupResult
{
    private StudentLookupResult(StudentSqlRecord? student, SqlStoredProcedureError? error)
    {
        Student = student;
        Error = error;
    }

    public StudentSqlRecord? Student { get; }

    public SqlStoredProcedureError? Error { get; }

    public bool Found => Student is not null;

    public bool HasStoredProcedureError => Error is not null;

    public static StudentLookupResult FoundStudent(StudentSqlRecord student) => new(student, null);

    public static StudentLookupResult NotFound() => new(null, null);

    public static StudentLookupResult StoredProcedureError(SqlStoredProcedureError error) => new(null, error);
}
