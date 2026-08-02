using System;
using System.ComponentModel.DataAnnotations;

namespace PGKing.Application.Entities
{
    public class GalleryItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Type of media: "Photo" or "Video"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string MediaType { get; set; } = "Photo"; // "Photo" or "Video"

        /// <summary>
        /// Category/Tag: "Rooms", "Community", "Events", "Dining", "Amenities"
        /// </summary>
        [MaxLength(50)]
        public string Category { get; set; } = "Rooms";

        /// <summary>
        /// Image URL or Video URL (MP4 file path or YouTube/Vimeo embed URL)
        /// </summary>
        [Required(ErrorMessage = "Media URL or uploaded file is required")]
        [MaxLength(1000)]
        public string MediaUrl { get; set; } = string.Empty;

        /// <summary>
        /// Optional Thumbnail URL for videos
        /// </summary>
        [MaxLength(1000)]
        public string? ThumbnailUrl { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
