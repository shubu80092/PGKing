using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PGKing.Application.Entities
{
    public class Flat
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty; // e.g. "Flat 101", "A-Wing 202"

        [Required]
        public string BhkType { get; set; } = string.Empty; // 1 BHK, 2 BHK, 3 BHK

        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public Property? Property { get; set; }

        public ICollection<PGRoom> Rooms { get; set; } = new List<PGRoom>();
        
        public ICollection<FlatMedia> Media { get; set; } = new List<FlatMedia>();
    }

    public class FlatMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string MediaType { get; set; } = "Image"; // Image or Video

        public int FlatId { get; set; }
        [ForeignKey("FlatId")]
        public Flat? Flat { get; set; }
    }
}
