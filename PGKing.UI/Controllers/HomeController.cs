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

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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
                .Include(p => p.Rooms)
                .Take(6)
                .ToListAsync();

            ViewBag.Banners = banners;
            ViewBag.Properties = properties;

            return View();
        }

        public async Task<IActionResult> Properties(string search = "", int? cityId = null)
        {
            var query = _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Rooms)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Title.Contains(search) || p.Address.Contains(search));
            }

            if (cityId.HasValue)
            {
                query = query.Where(p => p.CityId == cityId.Value);
            }

            var properties = await query.ToListAsync();
            ViewBag.Cities = await _context.Cities.ToListAsync();
            
            return View(properties);
        }

        public async Task<IActionResult> PropertyDetails(int id)
        {
            var property = await _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Media) // Include property gallery
                .Include(p => p.Rooms)
                    .ThenInclude(r => r.Media) // Include room gallery
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

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
