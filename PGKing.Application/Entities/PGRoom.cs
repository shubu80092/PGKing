using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

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

        public int? FlatId { get; set; }
        [ForeignKey("FlatId")]
        public Flat? Flat { get; set; }
        
        public string Amenities { get; set; } = string.Empty; // Comma separated list like "AC,Attached Washroom,Bed"

        public bool IsOccupied { get; set; } = false;

        // Client assignment fields
        public string? OccupiedByName { get; set; }
        public string? OccupiedByMobile { get; set; }
        public string? OccupiedByEmail { get; set; }
        public string? OccupiedByAadhar { get; set; }
        public string? OccupiedByEmergencyContact { get; set; }
        public string? OccupiedByAddress { get; set; }
        public DateTime? OccupiedSince { get; set; }
        
        [JsonIgnore]
        public ICollection<RoomMedia> Media { get; set; } = new List<RoomMedia>();
    }
}
