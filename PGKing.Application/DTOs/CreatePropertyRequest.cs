using System;
using System.ComponentModel.DataAnnotations;

namespace PGKing.Application.DTOs
{
    public class CreatePropertyRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public int StateId { get; set; }

        [Required]
        public int CityId { get; set; }

        public string Description { get; set; } = string.Empty;
        
        public string Amenities { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }
    }
}
