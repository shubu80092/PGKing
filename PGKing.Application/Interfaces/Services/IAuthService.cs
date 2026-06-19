using System.Threading.Tasks;
using PGKing.Application.DTOs;

namespace PGKing.Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResultDto> LoginAsync(LoginRequest request);
        Task<AuthResultDto> RefreshTokenAsync(RefreshTokenRequestDto request);
        Task<bool> ChangePasswordAsync(ChangePasswordDto request, string userId, string role);
    }
}
