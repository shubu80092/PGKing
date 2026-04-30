using System.ComponentModel.DataAnnotations;

namespace PGKing.Application.Entities
{
    public class Banner
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string SubTitle { get; set; } = string.Empty;

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
        
        public int DisplayOrder { get; set; } = 0;
    }
}
