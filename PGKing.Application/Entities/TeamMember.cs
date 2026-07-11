using System.ComponentModel.DataAnnotations;

namespace PGKing.Application.Entities
{
    public class TeamMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Designation { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? Bio { get; set; }

        public string? LinkedInUrl { get; set; }

        public string? Email { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
