using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PGRoom> Rooms { get; set; } = new List<PGRoom>();
        
        public ICollection<PropertyMedia> Media { get; set; } = new List<PropertyMedia>();
    }
}
