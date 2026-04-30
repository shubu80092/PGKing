using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PGKing.Application.Entities
{
    public class PGRoom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string SharingType { get; set; } = string.Empty;

        [Required]
        public decimal Rent { get; set; }

        [Required]
        public decimal Deposit { get; set; }

        public int PropertyId { get; set; }
        [ForeignKey("PropertyId")]
        public Property? Property { get; set; }
        
        public string Amenities { get; set; } = string.Empty; // Comma separated list like "AC,Attached Washroom,Bed"

        public ICollection<RoomMedia> Media { get; set; } = new List<RoomMedia>();
    }
}
