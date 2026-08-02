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
        public DbSet<TeamMember> TeamMembers { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }
        public DbSet<ContactInquiry> ContactInquiries { get; set; }

        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<SuperAdmin> SuperAdmins { get; set; }
        public DbSet<GalleryItem> GalleryItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Unique Constraints
            modelBuilder.Entity<Vendor>().HasIndex(v => v.Email).IsUnique();
            modelBuilder.Entity<Tenant>().HasIndex(t => t.Email).IsUnique();
            modelBuilder.Entity<SuperAdmin>().HasIndex(s => s.Username).IsUnique();
            modelBuilder.Entity<Property>().HasIndex(p => new { p.LocationSlug, p.PropertySlug });
            
            // Seed Data
            modelBuilder.Entity<SuperAdmin>().HasData(
                new SuperAdmin 
                { 
                    Id = 1, 
                    Username = "superadmin", 
                    PasswordHash = "$2a$11$e.fW4f.M6.2y0yHnS1R4KOW7Zc/9c5L31fH8y/6sH.Ld8UvG4B9XG", // BCrypt hash of "admin123"
                    CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );


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

            modelBuilder.Entity<TeamMember>().HasData(
                new TeamMember
                {
                    Id = 1,
                    Name = "Prahlad",
                    Designation = "Founder & CEO",
                    Bio = "Driving the vision to standardize premium, high-quality PG accommodations across India.",
                    ImageUrl = "https://images.unsplash.com/photo-1560250097-0b93528c311a?w=600",
                    Email = "pgkingmumbai@pgking.in",
                    DisplayOrder = 1,
                    IsActive = true
                },
                new TeamMember
                {
                    Id = 2,
                    Name = "Sneha Sharma",
                    Designation = "Head of Operations",
                    Bio = "Ensuring seamless property onboarding, regular quality maintenance, and tenant check-ins.",
                    ImageUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=600",
                    Email = "pgkingmumbai@pgking.in",
                    DisplayOrder = 2,
                    IsActive = true
                },
                new TeamMember
                {
                    Id = 3,
                    Name = "Rahul Verma",
                    Designation = "Customer Relations",
                    Bio = "Dedicated to handling student and professional booking support, inquiries, and reviews.",
                    ImageUrl = "https://images.unsplash.com/photo-1519085360753-af0119f7cbe7?w=600",
                    Email = "pgkingmumbai@pgking.in",
                    DisplayOrder = 3,
                    IsActive = true
                }
            );
        }
    }
}
