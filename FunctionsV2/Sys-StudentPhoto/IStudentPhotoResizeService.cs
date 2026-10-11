using System.Collections.Generic;
using System.Threading.Tasks;

namespace FunctionsV2.Sys_StudentPhoto.Services
{
    public interface IStudentPhotoResizeService
    {
        Task<ResizeResult> ResizeAndUpsertAsync(string stuCode, string photoPayload, string photoFileName);
        Task<ResizeResult> ResizeAndUpsertFromDbPhotoAsync(string stuCode, byte[] dbPhoto, string photoFileName);
        Task<IReadOnlyList<(string StuCode, byte[] Photo, string PhotoFileName)>> GetUnresizedBatchAsync(int top);
    }

    public record ResizeResult(string StuCode, string Operation, int ResizedBytes);
}