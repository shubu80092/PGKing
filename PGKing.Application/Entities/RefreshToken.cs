using System;
using System.ComponentModel.DataAnnotations;

namespace PGKing.Application.Entities
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public string JwtId { get; set; } = string.Empty;

        public bool IsUsed { get; set; }
        public bool IsRevoked { get; set; }

        public DateTime AddedDate { get; set; }
        public DateTime ExpiryDate { get; set; }

        // Optional: link to a specific user
        public int? VendorId { get; set; }
        public int? TenantId { get; set; }
    }
}
