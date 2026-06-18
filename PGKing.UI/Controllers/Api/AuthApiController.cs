using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PGKing.Application.DTOs;

namespace PGKing.UI.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (request.Email == "superadmin" && request.Password == "admin123")
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtSecret = _configuration["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_FOR_JWT_THAT_IS_LONG_ENOUGH_123!";
                var key = Encoding.ASCII.GetBytes(jwtSecret);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "1"),
                        new Claim(ClaimTypes.Name, request.Email),
                        new Claim(ClaimTypes.Role, "SuperAdmin")
                    }),
                    Expires = DateTime.UtcNow.AddHours(24),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                var response = new LoginResponse
                {
                    Token = tokenString,
                    Email = request.Email,
                    Role = "SuperAdmin"
                };

                return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful"));
            }

            return Unauthorized(ApiResponse<object>.Fail("Invalid Username or Password"));
        }
    }
}
