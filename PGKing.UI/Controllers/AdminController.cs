using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Infrastructure.Data;
using System.IO;
using PGKing.UI.Services;

namespace PGKing.UI.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;

        public AdminController(ApplicationDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        #region Dashboard
        public async Task<IActionResult> Dashboard()
        {
            int propertiesCount = 0;
            int flatsCount = 0;
            int totalBeds = 0;
            int occupiedBeds = 0;
            int pendingBookings = 0;
            decimal monthlyRevenue = 0;

            try
            {
                propertiesCount = await _context.Properties.CountAsync();
                flatsCount = await _context.Flats.CountAsync();
                
                var rooms = await _context.PGRooms.ToListAsync();
                totalBeds = rooms.Count;
                occupiedBeds = rooms.Count(r => r.IsOccupied);
                
                pendingBookings = await _context.Bookings.CountAsync(b => b.Status == "Pending");
                
                // Real monthly revenue from occupied beds
                monthlyRevenue = rooms.Where(r => r.IsOccupied).Sum(r => r.Rent);
            }
            catch (Exception ex)
            {
                // Log error if needed
            }

            ViewBag.PropertiesCount = propertiesCount;
            ViewBag.FlatsCount = flatsCount;
            ViewBag.TotalBeds = totalBeds;
            ViewBag.OccupiedBeds = occupiedBeds;
            ViewBag.AvailableBeds = totalBeds - occupiedBeds;
            ViewBag.PendingBookingsCount = pendingBookings;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            
            // Recent bookings
            var recentBookings = await _context.Bookings
                .Include(b => b.Property)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();
            
            return View(recentBookings);
        }
        #endregion

        #region Properties
        public async Task<IActionResult> Properties()
        {
            try
            {
                var properties = await _context.Properties
                    .Include(p => p.Flats)
                    .Include(p => p.City)
                    .Include(p => p.State)
                    .ToListAsync();
                return View(properties);
            }
            catch
            {
                return View(new List<Property>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> CreateProperty()
        {
            ViewBag.States = new SelectList(await _context.States.ToListAsync(), "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProperty(Property property, IFormFile? imageFile)
        {
            ModelState.Remove("ImageUrl");
            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null)
                    {
                        property.ImageUrl = await _storageService.SaveFileAsync(imageFile, "properties");
                    }

                    _context.Properties.Add(property);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(ManageProperty), new { id = property.Id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Database error: " + ex.Message);
                }
            }
            ViewBag.States = new SelectList(await _context.States.ToListAsync(), "Id", "Name");
            return View(property);
        }

        [HttpGet]
        public async Task<IActionResult> EditProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null) return NotFound();

            ViewBag.States = new SelectList(await _context.States.ToListAsync(), "Id", "Name", property.StateId);
            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.StateId == property.StateId).ToListAsync(), "Id", "Name", property.CityId);
            
            return View(property);
        }

        [HttpPost]
        public async Task<IActionResult> EditProperty(Property property, IFormFile? imageFile)
        {
            ModelState.Remove("ImageUrl");
            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null)
                    {
                        if (!string.IsNullOrEmpty(property.ImageUrl))
                        {
                            await _storageService.DeleteFileAsync(property.ImageUrl);
                        }
                        property.ImageUrl = await _storageService.SaveFileAsync(imageFile, "properties");
                    }

                    _context.Properties.Update(property);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Properties));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Database error: " + ex.Message);
                }
            }
            ViewBag.States = new SelectList(await _context.States.ToListAsync(), "Id", "Name", property.StateId);
            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.StateId == property.StateId).ToListAsync(), "Id", "Name", property.CityId);
            return View(property);
        }

        [HttpGet]
        public async Task<IActionResult> ManageProperty(int id)
        {
            var property = await _context.Properties
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                        .ThenInclude(r => r.Media)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media)
                .Include(p => p.Media)
                .Include(p => p.City)
                .Include(p => p.State)
                .FirstOrDefaultAsync(p => p.Id == id);
            
            if (property == null) return NotFound();
            return View(property);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePropertyAbout(int propertyId, string description)
        {
            var property = await _context.Properties.FindAsync(propertyId);
            if (property != null)
            {
                property.Description = description ?? "";
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> AddGalleryImages(int propertyId, List<IFormFile> galleryFiles)
        {
            if (galleryFiles != null && galleryFiles.Count > 0)
            {
                foreach (var file in galleryFiles)
                {
                    var filePath = await _storageService.SaveFileAsync(file, "properties/gallery");
                    _context.PropertyMedias.Add(new PropertyMedia
                    {
                        PropertyId = propertyId,
                        FilePath = filePath
                    });
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePropertyMedia(int id, int propertyId)
        {
            var media = await _context.PropertyMedias.FindAsync(id);
            if (media != null)
            {
                await _storageService.DeleteFileAsync(media.FilePath);
                _context.PropertyMedias.Remove(media);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> AddFlat(int propertyId, string flatName, string bhkType, List<IFormFile> mediaFiles)
        {
            var flat = new Flat
            {
                PropertyId = propertyId,
                Name = flatName,
                BhkType = bhkType
            };

            _context.Flats.Add(flat);
            await _context.SaveChangesAsync();

            if (mediaFiles != null && mediaFiles.Count > 0)
            {
                foreach (var file in mediaFiles)
                {
                    var filePath = await _storageService.SaveFileAsync(file, "flats");
                    var mediaType = file.ContentType.StartsWith("video/") ? "Video" : "Image";

                    _context.FlatMedias.Add(new FlatMedia
                    {
                        FlatId = flat.Id,
                        FilePath = filePath,
                        MediaType = mediaType
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> AddRoom(PGRoom room, List<string> selectedAmenities, int propertyId)
        {
            if (selectedAmenities != null && selectedAmenities.Any())
            {
                room.Amenities = string.Join(",", selectedAmenities);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.PGRooms.Add(room);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(ManageProperty), new { id = propertyId, error = "Database Error" });
                }
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFlat(int id, int propertyId)
        {
            var flat = await _context.Flats
                .Include(f => f.Rooms)
                .Include(f => f.Media)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flat != null)
            {
                foreach (var m in flat.Media)
                {
                    await _storageService.DeleteFileAsync(m.FilePath);
                }
                _context.Flats.Remove(flat);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoom(int id, int propertyId)
        {
            var room = await _context.PGRooms.Include(r => r.Media).FirstOrDefaultAsync(r => r.Id == id);
            if (room != null)
            {
                foreach (var m in room.Media)
                {
                    await _storageService.DeleteFileAsync(m.FilePath);
                }
                _context.PGRooms.Remove(room);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRoomStatus(int roomId, int propertyId)
        {
            var room = await _context.PGRooms.FindAsync(roomId);
            if (room != null)
            {
                room.IsOccupied = !room.IsOccupied;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }
        #endregion

        #region Banners
        public async Task<IActionResult> Banners()
        {
            var banners = await _context.Banners.OrderBy(b => b.DisplayOrder).ToListAsync();
            return View(banners);
        }

        [HttpGet]
        public IActionResult CreateBanner()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBanner(Banner banner, IFormFile imageFile)
        {
            ModelState.Remove("ImageUrl");
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    banner.ImageUrl = await _storageService.SaveFileAsync(imageFile, "banners");
                }
                _context.Banners.Add(banner);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Banners));
            }
            return View(banner);
        }

        [HttpGet]
        public async Task<IActionResult> EditBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost]
        public async Task<IActionResult> EditBanner(Banner banner, IFormFile? imageFile)
        {
            ModelState.Remove("ImageUrl");
            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    if (!string.IsNullOrEmpty(banner.ImageUrl))
                    {
                        await _storageService.DeleteFileAsync(banner.ImageUrl);
                    }
                    banner.ImageUrl = await _storageService.SaveFileAsync(imageFile, "banners");
                }
                _context.Banners.Update(banner);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Banners));
            }
            return View(banner);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                await _storageService.DeleteFileAsync(banner.ImageUrl);
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Banners));
        }
        #endregion

        #region Bookings
        public async Task<IActionResult> Bookings()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Property)
                .Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(int id, string status)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking != null)
            {
                booking.Status = status;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Bookings));
        }
        #endregion

        #region Helpers
        [HttpGet]
        public async Task<JsonResult> GetCitiesByState(int stateId)
        {
            var cities = await _context.Cities
                .Where(c => c.StateId == stateId)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();
            return Json(cities);
        }

        // File helper methods have been moved to StorageService
        #endregion
    }
}
