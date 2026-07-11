using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Infrastructure.Data;
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

                query = query.Where(p => p.Title.Contains(keyword) || p.Description.Contains(keyword));
            }

            var properties = await query.ToListAsync();
            ViewBag.Cities = await _context.Cities.ToListAsync();
            ViewBag.CurrentCityId = cityId;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPgType = pgType;
            
            return View(properties);
        }

        public async Task<IActionResult> PropertyDetails(int id)
        {
            var property = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Media) // Include property gallery
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media) // Include flat gallery
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return NotFound();

            return View(property);
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
                    "info@pgking.in",
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
