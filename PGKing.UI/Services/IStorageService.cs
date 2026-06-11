using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PGKing.UI.Services
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string subFolder);
        Task DeleteFileAsync(string fileUrl);
    }
}
