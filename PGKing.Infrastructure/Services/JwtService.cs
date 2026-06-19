using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PGKing.Application.DTOs;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Services;
using PGKing.Infrastructure.Data;

namespace PGKing.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public JwtService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<AuthResultDto> GenerateTokensAsync(string userId, string email, string role, int? vendorId = null, int? tenantId = null)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var secret = _configuration["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_FOR_JWT_THAT_IS_LONG_ENOUGH_123!";
            var key = Encoding.ASCII.GetBytes(secret);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.Role, role)
            };

            if (vendorId.HasValue)
                claims.Add(new Claim("VendorId", vendorId.Value.ToString()));
            
            if (tenantId.HasValue)
                claims.Add(new Claim("TenantId", tenantId.Value.ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60")),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                IsUsed = false,
                IsRevoked = false,
                VendorId = vendorId,
                TenantId = tenantId,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(1),
                Token = RandomString(35) + Guid.NewGuid().ToString()
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResultDto
            {
                Token = jwtToken,
                Success = true,
                RefreshToken = refreshToken.Token,
                Role = role
            };
        }

        public async Task<AuthResultDto> VerifyAndGenerateNewTokensAsync(RefreshTokenRequestDto request)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var secret = _configuration["Jwt:Secret"] ?? "YOUR_SUPER_SECRET_KEY_FOR_JWT_THAT_IS_LONG_ENOUGH_123!";
            var key = Encoding.ASCII.GetBytes(secret);

            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false // Here we intentionally don't validate lifetime to refresh an expired token
                };

                // Validate the JWT
                var principal = jwtTokenHandler.ValidateToken(request.Token, tokenValidationParameters, out var validatedToken);
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (!result)
                        return new AuthResultDto { Success = false, Errors = new[] { "Invalid token algorithm" } };
                }

                // Check refresh token existence
                var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == request.RefreshToken);
                if (storedToken == null)
                    return new AuthResultDto { Success = false, Errors = new[] { "Refresh token does not exist" } };

                // Validate refresh token status
                if (DateTime.UtcNow > storedToken.ExpiryDate)
                    return new AuthResultDto { Success = false, Errors = new[] { "Refresh token has expired" } };
                if (storedToken.IsUsed)
                    return new AuthResultDto { Success = false, Errors = new[] { "Refresh token has been used" } };
                if (storedToken.IsRevoked)
                    return new AuthResultDto { Success = false, Errors = new[] { "Refresh token has been revoked" } };

                // Validate JwtId matches
                var jti = principal.Claims.SingleOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;
                if (storedToken.JwtId != jti)
                    return new AuthResultDto { Success = false, Errors = new[] { "Token mismatch" } };

                // Update current token
                storedToken.IsUsed = true;
                _context.RefreshTokens.Update(storedToken);
                await _context.SaveChangesAsync();

                // Extract claims for new token
                var userId = principal.Claims.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
                var email = principal.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Name)?.Value ?? "";
                var role = principal.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Role)?.Value ?? "";
                
                int? vendorId = null;
                var vendorClaim = principal.Claims.SingleOrDefault(x => x.Type == "VendorId")?.Value;
                if (int.TryParse(vendorClaim, out var vId)) vendorId = vId;

                int? tenantId = null;
                var tenantClaim = principal.Claims.SingleOrDefault(x => x.Type == "TenantId")?.Value;
                if (int.TryParse(tenantClaim, out var tId)) tenantId = tId;

                // Generate new tokens
                return await GenerateTokensAsync(userId, email, role, vendorId, tenantId);
            }
            catch (Exception ex)
            {
                return new AuthResultDto { Success = false, Errors = new[] { ex.Message } };
            }
        }

        private string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
