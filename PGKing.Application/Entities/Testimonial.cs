using System.ComponentModel.DataAnnotations;

namespace PGKing.Application.Entities
{
    public class Testimonial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Designation { get; set; }

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; } = 5;

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }
}
