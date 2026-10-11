using FunctionsV2.Sys_StudentApp.Models;

namespace FunctionsV2.Sys_StudentApp.Services;

public interface IStudentResponseMapper
{
    StudentAppResponse Map(StudentSqlRecord record);
}
