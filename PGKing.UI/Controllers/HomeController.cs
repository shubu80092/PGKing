using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Infrastructure.Data;
using PGKing.UI.Helpers;
using System.Diagnostics;

namespace PGKing.UI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly PGKing.Application.Interfaces.Services.IEmailService _emailService;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, PGKing.Application.Interfaces.Services.IEmailService emailService)
        {
            _logger = logger;
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var banners = await _context.Banners
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();

            var properties = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Media)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media)
                .Take(6)
                .ToListAsync();

            ViewBag.Banners = banners;
            ViewBag.Properties = properties;
            ViewBag.Cities = await _context.Cities.ToListAsync();
            ViewBag.Testimonials = await _context.Testimonials.Where(t => t.IsActive).OrderBy(t => t.DisplayOrder).ToListAsync();
            ViewBag.GalleryItems = await _context.GalleryItems.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenByDescending(g => g.Id).Take(6).ToListAsync();

            return View();
        }

        public async Task<IActionResult> Properties(string search = "", int? cityId = null, string pgType = "")
        {
            var query = _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Media)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || 
                                         p.Address.Contains(search) || 
                                         (p.City != null && p.City.Name.Contains(search)) || 
                                         (p.State != null && p.State.Name.Contains(search)));
            }

            if (cityId.HasValue)
            {
                query = query.Where(p => p.CityId == cityId.Value);
            }

            if (!string.IsNullOrEmpty(pgType) && pgType != "Any PG Type")
            {
                string keyword = pgType;
                if (pgType.Contains("Boys")) keyword = "Boys";
                else if (pgType.Contains("Girls")) keyword = "Girls";
                else if (pgType.Contains("Co-living")) keyword = "Co-living";

                query = query.Where(p => p.PgType.Contains(keyword) || p.Title.Contains(keyword) || p.Description.Contains(keyword));
            }

            var properties = await query.ToListAsync();
            ViewBag.Cities = await _context.Cities.ToListAsync();
            ViewBag.CurrentCityId = cityId;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPgType = pgType;
            
            return View(properties);
        }

        // 1. Existing (old) route handler: permanently redirect 301 to new SEO canonical URL without chains
        public async Task<IActionResult> PropertyDetailsOld(string slug = "")
        {
            int targetId = 0;
            if (!string.IsNullOrEmpty(slug))
            {
                var lastDashIndex = slug.LastIndexOf('-');
                if (lastDashIndex != -1)
                {
                    var idStr = slug.Substring(lastDashIndex + 1);
                    int.TryParse(idStr, out targetId);
                }
            }

            Property? property = null;
            if (targetId > 0)
            {
                property = await _context.Properties.Include(p => p.City).FirstOrDefaultAsync(p => p.Id == targetId);
            }
            if (property == null && !string.IsNullOrEmpty(slug))
            {
                property = await _context.Properties.Include(p => p.City).FirstOrDefaultAsync(p => p.PropertySlug == slug || p.Title.ToLower() == slug.ToLower());
            }

            if (property == null) return NotFound();

            var locSlug = !string.IsNullOrEmpty(property.LocationSlug) ? property.LocationSlug : SeoHelper.GenerateLocationSlug(property.Area, property.CityName ?? property.City?.Name);
            var propSlug = !string.IsNullOrEmpty(property.PropertySlug) ? property.PropertySlug : SeoHelper.GenerateSlug(property.Title);

            return RedirectPermanent($"/{locSlug}/{propSlug}");
        }

        // 2. SEO Location Listing action: /pg-in-{area}-{city}
        public async Task<IActionResult> LocationPropertiesSeo(string locationSlug)
        {
            var properties = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Include(p => p.Media)
                .ToListAsync();

            // Match by LocationSlug e.g. pg-in-bhandup-west-mumbai
            var filtered = properties.Where(p =>
            {
                var pLoc = !string.IsNullOrEmpty(p.LocationSlug) ? p.LocationSlug : SeoHelper.GenerateLocationSlug(p.Area, p.CityName ?? p.City?.Name);
                return string.Equals(pLoc, "pg-in-" + locationSlug, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(pLoc, locationSlug, StringComparison.OrdinalIgnoreCase);
            }).ToList();

            string areaDisplay = "Mumbai";
            string cityDisplay = "Mumbai";

            var first = filtered.FirstOrDefault();
            if (first != null)
            {
                areaDisplay = !string.IsNullOrEmpty(first.Area) ? first.Area : (first.City?.Name ?? "Mumbai");
                cityDisplay = !string.IsNullOrEmpty(first.CityName) ? first.CityName : (first.City?.Name ?? "Mumbai");
            }
            else if (!string.IsNullOrEmpty(locationSlug))
            {
                var clean = locationSlug.Replace("pg-in-", "", StringComparison.OrdinalIgnoreCase).Replace("-", " ");
                areaDisplay = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clean);
            }

            ViewData["Title"] = $"PG in {areaDisplay}, {cityDisplay} | PG Rooms for Rent | PGKing";
            ViewData["H1Title"] = $"Best PG in {areaDisplay}, {cityDisplay}";
            ViewData["CanonicalUrl"] = $"https://pgking.in/{(locationSlug.StartsWith("pg-in-") ? locationSlug : "pg-in-" + locationSlug)}";
            ViewBag.Cities = await _context.Cities.ToListAsync();

            return View("Properties", filtered);
        }

        // 3. Canonical SEO Property Details action: /pg-in-{area}-{city}/{propertySlug}
        public async Task<IActionResult> PropertyDetailsSeo(string locationSlug, string propertySlug, string error = "")
        {
            var property = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Media)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media)
                .FirstOrDefaultAsync(p => p.LocationSlug == locationSlug && p.PropertySlug == propertySlug);

            if (property == null)
            {
                // Fallback attempt by propertySlug alone
                property = await _context.Properties
                    .Include(p => p.City)
                    .Include(p => p.State)
                    .Include(p => p.Media)
                    .Include(p => p.Flats)
                        .ThenInclude(f => f.Rooms)
                    .Include(p => p.Flats)
                        .ThenInclude(f => f.Media)
                    .FirstOrDefaultAsync(p => p.PropertySlug == propertySlug);

                if (property == null) return NotFound();

                // 301 permanent redirect to correct canonical locationSlug
                var correctLocSlug = !string.IsNullOrEmpty(property.LocationSlug) ? property.LocationSlug : SeoHelper.GenerateLocationSlug(property.Area, property.CityName ?? property.City?.Name);
                var correctPropSlug = !string.IsNullOrEmpty(property.PropertySlug) ? property.PropertySlug : SeoHelper.GenerateSlug(property.Title);
                return RedirectPermanent($"/{correctLocSlug}/{correctPropSlug}");
            }

            var locSlug = !string.IsNullOrEmpty(property.LocationSlug) ? property.LocationSlug : SeoHelper.GenerateLocationSlug(property.Area, property.CityName ?? property.City?.Name);
            var propSlug = !string.IsNullOrEmpty(property.PropertySlug) ? property.PropertySlug : SeoHelper.GenerateSlug(property.Title);

            ViewData["CanonicalUrl"] = property.CanonicalUrl ?? SeoHelper.GenerateCanonicalUrl(locSlug, propSlug);
            var cityName = property.CityName ?? property.City?.Name ?? "Mumbai";
            var areaName = !string.IsNullOrEmpty(property.Area) ? property.Area : cityName;

            ViewData["Title"] = $"{property.Title} – PG in {areaName}, {cityName} | PgKing";
            ViewData["MetaDescription"] = $"Find {property.Title} in {areaName}, {cityName}. Check rent, room details, facilities, photos, availability and contact information on PgKing.";
            ViewData["H1Title"] = $"{property.Title} in {areaName}, {cityName}";

            ViewBag.Error = error;
            return View("PropertyDetails", property);
        }

        // 4. Backward-compatible internal action for PropertyDetails by ID or slug
        public async Task<IActionResult> PropertyDetails(int? id, string slug = "", string error = "")
        {
            if (id.HasValue)
            {
                var property = await _context.Properties.Include(p => p.City).FirstOrDefaultAsync(p => p.Id == id.Value);
                if (property != null)
                {
                    var locSlug = !string.IsNullOrEmpty(property.LocationSlug) ? property.LocationSlug : SeoHelper.GenerateLocationSlug(property.Area, property.CityName ?? property.City?.Name);
                    var propSlug = !string.IsNullOrEmpty(property.PropertySlug) ? property.PropertySlug : SeoHelper.GenerateSlug(property.Title);
                    return RedirectPermanent($"/{locSlug}/{propSlug}");
                }
            }

            return await PropertyDetailsOld(slug);
        }

        private string GeneratePropertySlug(Property property)
        {
            return !string.IsNullOrEmpty(property.PropertySlug) ? property.PropertySlug : SeoHelper.GenerateSlug(property.Title);
        }

        [HttpPost]
        public async Task<IActionResult> BookRoom(int propertyId, int roomId, string fullName, string mobileNumber)
        {
            if (string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(mobileNumber))
            {
                return RedirectToAction(nameof(PropertyDetails), new { id = propertyId, error = "Please fill all details" });
            }

            var booking = new Booking
            {
                PropertyId = propertyId,
                PGRoomId = roomId,
                FullName = fullName,
                MobileNumber = mobileNumber,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(BookingSuccess));
        }

        public IActionResult BookingSuccess()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Services()
        {
            return View();
        }

        public async Task<IActionResult> Team()
        {
            try
            {
                await _context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automatic migration failed in HomeController.Team");
            }

            var members = await _context.TeamMembers
                .Where(t => t.IsActive)
                .OrderBy(t => t.DisplayOrder)
                .ToListAsync();
            return View(members);
        }

        [HttpGet]
        public async Task<IActionResult> Contact()
        {
            try
            {
                await _context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automatic migration failed in HomeController.Contact [GET]");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Contact(string name, string phone, string email, string message)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(message))
            {
                TempData["Error"] = "Please fill in all fields.";
                return View();
            }

            var inquiry = new ContactInquiry
            {
                Name = name,
                Phone = phone,
                Email = email,
                Message = message,
                CreatedAt = DateTime.Now
            };

            _context.ContactInquiries.Add(inquiry);
            await _context.SaveChangesAsync();

            // Send Email Notification
            try
            {
                await _emailService.SendEmailAsync(
                    "pgkingmumbai@pgking.in",
                    "New Contact Inquiry - " + name,
                    $"<h3>New Contact Inquiry Received</h3>" +
                    $"<p><strong>Name:</strong> {name}</p>" +
                    $"<p><strong>Phone:</strong> {phone}</p>" +
                    $"<p><strong>Email:</strong> {email}</p>" +
                    $"<p><strong>Message:</strong> {message}</p>"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send contact inquiry notification email.");
            }

            TempData["Success"] = "Thank you! Your message has been received.";
            return RedirectToAction(nameof(Contact));
        }

        [HttpGet]
        public async Task<IActionResult> Gallery(string category = "All")
        {
            var query = _context.GalleryItems.Where(g => g.IsActive).AsQueryable();
            if (!string.IsNullOrEmpty(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (category.Equals("Photos", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(g => g.MediaType == "Photo");
                }
                else if (category.Equals("Videos", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(g => g.MediaType == "Video");
                }
                else
                {
                    query = query.Where(g => g.Category == category);
                }
            }

            var items = await query.OrderBy(g => g.DisplayOrder).ThenByDescending(g => g.Id).ToListAsync();
            ViewBag.CurrentCategory = category;
            ViewBag.Categories = new[] { "All", "Photos", "Videos", "Rooms", "Community", "Events", "Dining", "Amenities" };
            return View(items);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DbTest()
        {
            try
            {
                await _context.Database.MigrateAsync();
                return Content("Migration Successful! Active Team members count: " + await _context.TeamMembers.CountAsync());
            }
            catch (Exception ex)
            {
                return Content($"Migration Failed!\nMessage: {ex.Message}\nInnerException: {ex.InnerException?.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
