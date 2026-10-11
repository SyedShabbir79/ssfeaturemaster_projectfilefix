using FunctionsV2.Sys_StudentApp.Models;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_StudentApp.Services;

public interface IStudentRepository
{
    Task<StudentLookupResult> GetStudentAsync(string upn, CancellationToken cancellationToken);
}
