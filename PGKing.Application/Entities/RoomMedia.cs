using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PGKing.Application.Entities
{
    public class RoomMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string MediaType { get; set; } = string.Empty; // "Image" or "Video"

        public int PGRoomId { get; set; }

        [ForeignKey("PGRoomId")]
        public PGRoom? PGRoom { get; set; }
    }
}
