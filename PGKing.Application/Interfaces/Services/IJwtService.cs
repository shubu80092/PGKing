using System.Threading.Tasks;
using PGKing.Application.DTOs;

namespace PGKing.Application.Interfaces.Services
{
    public interface IJwtService
    {
        Task<AuthResultDto> GenerateTokensAsync(string userId, string email, string role, int? vendorId = null, int? tenantId = null);
        Task<AuthResultDto> VerifyAndGenerateNewTokensAsync(RefreshTokenRequestDto request);
    }
}
