using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace PGKing.Application.Entities
{
    public class Property
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public int StateId { get; set; }
        [ForeignKey("StateId")]
        public State? State { get; set; }

        public int CityId { get; set; }
        [ForeignKey("CityId")]
        public City? City { get; set; }

        public string Description { get; set; } = string.Empty;
        
        public string Amenities { get; set; } = string.Empty;

        public double? Latitude { get; set; }
        
        public double? Longitude { get; set; }

        [Required]
        [StringLength(50)]
        public string PgType { get; set; } = "Co-living";

        public bool IsVerified { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string? Area { get; set; }

        [StringLength(100)]
        public string? CityName { get; set; }

        [StringLength(100)]
        public string? StateName { get; set; }

        [StringLength(200)]
        public string? PropertySlug { get; set; }

        [StringLength(200)]
        public string? LocationSlug { get; set; }

        [StringLength(500)]
        public string? CanonicalUrl { get; set; }

        // Vendor ownership (nullable for SuperAdmin/system properties)
        public int? VendorId { get; set; }
        [ForeignKey("VendorId")]
        public Vendor? Vendor { get; set; }

        public ICollection<Flat> Flats { get; set; } = new List<Flat>();
        
        [JsonIgnore]
        public ICollection<PropertyMedia> Media { get; set; } = new List<PropertyMedia>();
    }
}
