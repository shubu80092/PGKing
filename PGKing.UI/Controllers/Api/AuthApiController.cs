using Microsoft.AspNetCore.Mvc;
using PGKing.Application.DTOs;
using PGKing.Application.Interfaces.Services;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace PGKing.UI.Controllers.Api
{
    [ApiController]
    [Route("api/Auth")]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthApiController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (result.Success)
                return Ok(ApiResponse<AuthResultDto>.Ok(result, "Login successful"));

            return Unauthorized(ApiResponse<object>.Fail(result.Errors.FirstOrDefault() ?? "Invalid credentials"));
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);
            if (result.Success)
                return Ok(ApiResponse<AuthResultDto>.Ok(result, "Token refreshed successfully"));

            return Unauthorized(ApiResponse<object>.Fail(result.Errors.FirstOrDefault() ?? "Invalid token"));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(role))
                return Unauthorized(ApiResponse<object>.Fail("Invalid token claims"));

            var success = await _authService.ChangePasswordAsync(request, userId, role);
            if (success)
                return Ok(ApiResponse<object>.Ok(null, "Password changed successfully"));

            return BadRequest(ApiResponse<object>.Fail("Failed to change password. Please check current password."));
        }
    }
}
