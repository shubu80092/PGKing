using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PGKing.Application.Entities
{
    public class State
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public ICollection<City> Cities { get; set; } = new List<City>();
    }
}
