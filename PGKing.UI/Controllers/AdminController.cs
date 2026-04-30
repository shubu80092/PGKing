using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Infrastructure.Data;
using System.IO;

namespace PGKing.UI.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        #region Dashboard
        public async Task<IActionResult> Dashboard()
        {
            int propertiesCount = 0;
            int bannersCount = 0;
            int bookingsCount = 0;
            try
            {
                propertiesCount = await _context.Properties.CountAsync();
                bannersCount = await _context.Banners.CountAsync();
                bookingsCount = await _context.Bookings.CountAsync(b => b.Status == "Pending");
            }
            catch
            {
                propertiesCount = 0;
                bannersCount = 0;
                bookingsCount = 0;
            }

            ViewBag.PropertiesCount = propertiesCount;
            ViewBag.BannersCount = bannersCount;
            ViewBag.PendingBookingsCount = bookingsCount;
            return View();
        }
        #endregion

        #region Properties
        public async Task<IActionResult> Properties()
        {
            try
            {
                var properties = await _context.Properties
                    .Include(p => p.Rooms)
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
                        property.ImageUrl = await SaveFile(imageFile, "properties");
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
                            DeleteFile(property.ImageUrl);
                        }
                        property.ImageUrl = await SaveFile(imageFile, "properties");
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
                .Include(p => p.Rooms)
                    .ThenInclude(r => r.Media)
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
                    var filePath = await SaveFile(file, "properties/gallery");
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
                DeleteFile(media.FilePath);
                _context.PropertyMedias.Remove(media);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> AddRoom(PGRoom room, List<IFormFile> mediaFiles, List<string> selectedAmenities)
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

                    if (mediaFiles != null && mediaFiles.Count > 0)
                    {
                        foreach (var file in mediaFiles)
                        {
                            var filePath = await SaveFile(file, "rooms");
                            var mediaType = file.ContentType.StartsWith("video/") ? "Video" : "Image";
                            
                            _context.RoomMedias.Add(new RoomMedia
                            {
                                PGRoomId = room.Id,
                                FilePath = filePath,
                                MediaType = mediaType
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(ManageProperty), new { id = room.PropertyId, error = "Database Error" });
                }
            }
            return RedirectToAction(nameof(ManageProperty), new { id = room.PropertyId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRoom(int id, int propertyId)
        {
            var room = await _context.PGRooms.Include(r => r.Media).FirstOrDefaultAsync(r => r.Id == id);
            if (room != null)
            {
                foreach (var m in room.Media)
                {
                    DeleteFile(m.FilePath);
                }
                _context.PGRooms.Remove(room);
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
                    banner.ImageUrl = await SaveFile(imageFile, "banners");
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
                        DeleteFile(banner.ImageUrl);
                    }
                    banner.ImageUrl = await SaveFile(imageFile, "banners");
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
                DeleteFile(banner.ImageUrl);
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

        private async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/" + subFolder + "/" + fileName;
        }

        private void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || filePath.StartsWith("http")) return;
            
            string fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        #endregion
    }
}
