using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;

namespace PGKing.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Property> Properties { get; set; }
        public DbSet<PGRoom> PGRooms { get; set; }
        public DbSet<Flat> Flats { get; set; }
        public DbSet<FlatMedia> FlatMedias { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<RoomMedia> RoomMedias { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<PropertyMedia> PropertyMedias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Seed Data
            modelBuilder.Entity<State>().HasData(
                new State { Id = 1, Name = "Maharashtra" },
                new State { Id = 2, Name = "Karnataka" },
                new State { Id = 3, Name = "Delhi" }
            );

            modelBuilder.Entity<City>().HasData(
                new City { Id = 1, Name = "Mumbai", StateId = 1 },
                new City { Id = 2, Name = "Pune", StateId = 1 },
                new City { Id = 3, Name = "Thane", StateId = 1 },
                new City { Id = 4, Name = "Navi Mumbai", StateId = 1 },
                new City { Id = 5, Name = "Bengaluru", StateId = 2 },
                new City { Id = 6, Name = "New Delhi", StateId = 3 }
            );
        }
    }
}
