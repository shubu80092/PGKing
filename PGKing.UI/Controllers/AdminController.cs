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
    [Authorize(Roles = "SuperAdmin,Vendor")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IStorageService _storageService;

        public AdminController(ApplicationDbContext context, IStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        private int? GetVendorId()
        {
            if (User.IsInRole("Vendor"))
            {
                var vendorIdClaim = User.Claims.FirstOrDefault(c => c.Type == "VendorId")?.Value;
                if (int.TryParse(vendorIdClaim, out int id))
                {
                    return id;
                }
            }
            return null;
        }

        private async Task<bool> CheckPropertyOwnershipAsync(int propertyId)
        {
            var property = await _context.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == propertyId);
            if (property == null) return false;
            
            int? vendorId = GetVendorId();
            if (vendorId.HasValue && property.VendorId != vendorId.Value)
            {
                return false;
            }
            return true;
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

            int? vendorId = GetVendorId();

            try
            {
                var propertiesQuery = _context.Properties.AsQueryable();
                if (vendorId.HasValue)
                {
                    propertiesQuery = propertiesQuery.Where(p => p.VendorId == vendorId.Value);
                }
                propertiesCount = await propertiesQuery.CountAsync();

                var flatsQuery = _context.Flats.AsQueryable();
                if (vendorId.HasValue)
                {
                    flatsQuery = flatsQuery.Where(f => f.Property.VendorId == vendorId.Value);
                }
                flatsCount = await flatsQuery.CountAsync();
                
                var roomsQuery = _context.PGRooms.AsQueryable();
                if (vendorId.HasValue)
                {
                    roomsQuery = roomsQuery.Where(r => r.Flat.Property.VendorId == vendorId.Value);
                }
                var rooms = await roomsQuery.ToListAsync();
                totalBeds = rooms.Count;
                occupiedBeds = rooms.Count(r => r.IsOccupied);
                
                var bookingsQuery = _context.Bookings.AsQueryable();
                if (vendorId.HasValue)
                {
                    bookingsQuery = bookingsQuery.Where(b => b.Property.VendorId == vendorId.Value);
                }
                pendingBookings = await bookingsQuery.CountAsync(b => b.Status == "Pending");
                
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
            
            // Per-property bed availability breakdown
            var statsQuery = _context.Properties.AsQueryable();
            if (vendorId.HasValue)
            {
                statsQuery = statsQuery.Where(p => p.VendorId == vendorId.Value);
            }
            var propertyBedStats = await statsQuery
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Select(p => new 
                {
                    PropertyId = p.Id,
                    PropertyName = p.Title,
                    TotalBeds = p.Flats.SelectMany(f => f.Rooms).Count(),
                    OccupiedBeds = p.Flats.SelectMany(f => f.Rooms).Count(r => r.IsOccupied)
                })
                .ToListAsync();
            ViewBag.PropertyBedStats = propertyBedStats;
            
            // Recent bookings
            var rBookingsQuery = _context.Bookings.AsQueryable();
            if (vendorId.HasValue)
            {
                rBookingsQuery = rBookingsQuery.Where(b => b.Property.VendorId == vendorId.Value);
            }
            var recentBookings = await rBookingsQuery
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
                int? vendorId = GetVendorId();
                var query = _context.Properties.AsQueryable();
                if (vendorId.HasValue)
                {
                    query = query.Where(p => p.VendorId == vendorId.Value);
                }
                var properties = await query
                    .Include(p => p.Flats)
                        .ThenInclude(f => f.Rooms)
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

                    // Assign property to the logged-in Vendor
                    property.VendorId = GetVendorId();

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

            // Prevent Vendors from accessing other users' properties
            int? vendorId = GetVendorId();
            if (vendorId.HasValue && property.VendorId != vendorId.Value)
            {
                return Forbid();
            }

            ViewBag.States = new SelectList(await _context.States.ToListAsync(), "Id", "Name", property.StateId);
            ViewBag.Cities = new SelectList(await _context.Cities.Where(c => c.StateId == property.StateId).ToListAsync(), "Id", "Name", property.CityId);
            
            return View(property);
        }

        [HttpPost]
        public async Task<IActionResult> EditProperty(Property property, IFormFile? imageFile)
        {
            // Verify ownership
            var existingProperty = await _context.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == property.Id);
            if (existingProperty == null) return NotFound();

            int? vendorId = GetVendorId();
            if (vendorId.HasValue && existingProperty.VendorId != vendorId.Value)
            {
                return Forbid();
            }

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

                    property.VendorId = existingProperty.VendorId; // Retain ownership

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

            // Prevent Vendors from managing other users' properties
            int? vendorId = GetVendorId();
            if (vendorId.HasValue && property.VendorId != vendorId.Value)
            {
                return Forbid();
            }

            return View(property);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePropertyAbout(int propertyId, string description)
        {
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

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
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

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
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

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
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

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
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

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
        public async Task<IActionResult> EditRoom(PGRoom room, List<string> selectedAmenities, int propertyId)
        {
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

            if (selectedAmenities != null && selectedAmenities.Any())
            {
                room.Amenities = string.Join(",", selectedAmenities);
            }
            else
            {
                room.Amenities = string.Empty;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingRoom = await _context.PGRooms.FindAsync(room.Id);
                    if (existingRoom == null) return NotFound();

                    existingRoom.SharingType = room.SharingType;
                    existingRoom.Rent = room.Rent;
                    existingRoom.Deposit = room.Deposit;
                    existingRoom.Amenities = room.Amenities;

                    _context.PGRooms.Update(existingRoom);
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
        public async Task<IActionResult> EditFlat(Flat flat)
        {
            if (!await CheckPropertyOwnershipAsync(flat.PropertyId)) return Forbid();

            ModelState.Remove("Property");
            if (ModelState.IsValid)
            {
                try
                {
                    var existingFlat = await _context.Flats.FindAsync(flat.Id);
                    if (existingFlat == null) return NotFound();

                    existingFlat.Name = flat.Name;
                    existingFlat.BhkType = flat.BhkType;

                    _context.Flats.Update(existingFlat);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return RedirectToAction(nameof(ManageProperty), new { id = flat.PropertyId, error = "Database Error: " + ex.Message });
                }
            }
            return RedirectToAction(nameof(ManageProperty), new { id = flat.PropertyId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFlat(int id, int propertyId)
        {
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

            var flat = await _context.Flats
                .Include(f => f.Rooms)
                    .ThenInclude(r => r.Media)
                .Include(f => f.Media)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flat != null)
            {
                // Cascade delete Bookings for all rooms in this flat
                var roomIds = flat.Rooms.Select(r => r.Id).ToList();
                if (roomIds.Any())
                {
                    var bookings = await _context.Bookings.Where(b => roomIds.Contains(b.PGRoomId)).ToListAsync();
                    if (bookings.Any())
                    {
                        _context.Bookings.RemoveRange(bookings);
                    }
                }

                // Delete S3/local files for room media
                foreach (var room in flat.Rooms)
                {
                    foreach (var m in room.Media)
                    {
                        await _storageService.DeleteFileAsync(m.FilePath);
                    }
                }

                // Explicitly remove all PGRooms in this flat from the database
                if (flat.Rooms.Any())
                {
                    _context.PGRooms.RemoveRange(flat.Rooms);
                }

                // Delete S3/local files for flat media
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
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

            var room = await _context.PGRooms
                .Include(r => r.Media)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room != null)
            {
                // Cascade delete Bookings for this room
                var bookings = await _context.Bookings.Where(b => b.PGRoomId == id).ToListAsync();
                if (bookings.Any())
                {
                    _context.Bookings.RemoveRange(bookings);
                }

                // Delete associated media files
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
        public async Task<IActionResult> DeleteFlatMedia(int id, int propertyId)
        {
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

            var media = await _context.FlatMedias.FindAsync(id);
            if (media != null)
            {
                await _storageService.DeleteFileAsync(media.FilePath);
                _context.FlatMedias.Remove(media);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> AddFlatImages(int flatId, List<IFormFile> flatFiles, int propertyId)
        {
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

            if (flatFiles != null && flatFiles.Count > 0)
            {
                foreach (var file in flatFiles)
                {
                    var filePath = await _storageService.SaveFileAsync(file, "flats");
                    var mediaType = file.ContentType.StartsWith("video/") ? "Video" : "Image";

                    _context.FlatMedias.Add(new FlatMedia
                    {
                        FlatId = flatId,
                        FilePath = filePath,
                        MediaType = mediaType
                    });
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRoomStatus(int roomId, int propertyId)
        {
            if (!await CheckPropertyOwnershipAsync(propertyId)) return Forbid();

            var room = await _context.PGRooms.FindAsync(roomId);
            if (room != null)
            {
                room.IsOccupied = !room.IsOccupied;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(ManageProperty), new { id = propertyId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProperty(int id)
        {
            if (!await CheckPropertyOwnershipAsync(id)) return Forbid();

            var property = await _context.Properties
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                        .ThenInclude(r => r.Media)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media)
                .Include(p => p.Media)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null) return NotFound();

            var flats = property.Flats.ToList();
            var rooms = flats.SelectMany(f => f.Rooms).ToList();
            var roomIds = rooms.Select(r => r.Id).ToList();
            if (roomIds.Any())
            {
                var bookings = await _context.Bookings.Where(b => roomIds.Contains(b.PGRoomId)).ToListAsync();
                if (bookings.Any())
                {
                    _context.Bookings.RemoveRange(bookings);
                }
            }

            foreach (var r in rooms)
            {
                foreach (var rm in r.Media)
                {
                    await _storageService.DeleteFileAsync(rm.FilePath);
                }
            }

            foreach (var f in flats)
            {
                foreach (var fm in f.Media)
                {
                    await _storageService.DeleteFileAsync(fm.FilePath);
                }
            }

            foreach (var pm in property.Media)
            {
                await _storageService.DeleteFileAsync(pm.FilePath);
            }

            if (!string.IsNullOrEmpty(property.ImageUrl))
            {
                await _storageService.DeleteFileAsync(property.ImageUrl);
            }

            if (rooms.Any()) _context.PGRooms.RemoveRange(rooms);
            if (flats.Any()) _context.Flats.RemoveRange(flats);
            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Properties));
        }
        #endregion

        #region Banners
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Banners()
        {
            var banners = await _context.Banners.OrderBy(b => b.DisplayOrder).ToListAsync();
            return View(banners);
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult CreateBanner()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
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
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> EditBanner(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
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
        [Authorize(Roles = "SuperAdmin")]
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
            int? vendorId = GetVendorId();
            var query = _context.Bookings.AsQueryable();
            if (vendorId.HasValue)
            {
                query = query.Where(b => b.Property.VendorId == vendorId.Value);
            }
            var bookings = await query
                .Include(b => b.Property)
                .Include(b => b.Room)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
            return View(bookings);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBookingStatus(int id, string status)
        {
            var booking = await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == id);
            
            if (booking != null)
            {
                // Verify Vendor owns this property
                int? vendorId = GetVendorId();
                if (vendorId.HasValue && booking.Property.VendorId != vendorId.Value)
                {
                    return Forbid();
                }

                var previousStatus = booking.Status;
                booking.Status = status;

                // Auto-assign bed when booking is Confirmed
                if (status == "Confirmed" && booking.Room != null)
                {
                    booking.Room.IsOccupied = true;
                    booking.Room.OccupiedByName = booking.FullName;
                    booking.Room.OccupiedByMobile = booking.MobileNumber;
                    booking.Room.OccupiedSince = booking.Room.OccupiedSince ?? DateTime.Now;
                }
                // Auto-release bed when booking is Cancelled and it was previously Confirmed
                else if (status == "Cancelled" && previousStatus == "Confirmed" && booking.Room != null)
                {
                    booking.Room.IsOccupied = false;
                    booking.Room.OccupiedByName = null;
                    booking.Room.OccupiedByMobile = null;
                    booking.Room.OccupiedByEmail = null;
                    booking.Room.OccupiedByAadhar = null;
                    booking.Room.OccupiedByEmergencyContact = null;
                    booking.Room.OccupiedByAddress = null;
                    booking.Room.OccupiedSince = null;
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Bookings));
        }
        #endregion

        #region Clients / Tenants
        public async Task<IActionResult> Clients()
        {
            int? vendorId = GetVendorId();
            var query = _context.PGRooms.AsQueryable();
            if (vendorId.HasValue)
            {
                query = query.Where(r => r.Flat.Property.VendorId == vendorId.Value);
            }
            var rooms = await query
                .Include(r => r.Flat)
                    .ThenInclude(f => f.Property)
                .ToListAsync();
            return View(rooms);
        }

        [HttpPost]
        public async Task<IActionResult> AssignClientToBed(int roomId, string clientName, string clientMobile, string clientEmail, string clientAadhar, string emergencyContact, string permanentAddress, DateTime? moveInDate)
        {
            var room = await _context.PGRooms
                .Include(r => r.Flat)
                    .ThenInclude(f => f.Property)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room != null)
            {
                // Verify Vendor owns this property
                int? vendorId = GetVendorId();
                if (vendorId.HasValue && room.Flat?.Property?.VendorId != vendorId.Value)
                {
                    return Forbid();
                }

                room.IsOccupied = true;
                room.OccupiedByName = clientName;
                room.OccupiedByMobile = clientMobile;
                room.OccupiedByEmail = clientEmail;
                room.OccupiedByAadhar = clientAadhar;
                room.OccupiedByEmergencyContact = emergencyContact;
                room.OccupiedByAddress = permanentAddress;
                room.OccupiedSince = moveInDate ?? DateTime.Now;

                // Auto-create Tenant Credentials if Email is provided
                if (!string.IsNullOrEmpty(clientEmail))
                {
                    var existingTenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Email == clientEmail);
                    if (existingTenant == null)
                    {
                        int tenantVendorId = vendorId ?? 1;
                        if (!vendorId.HasValue)
                        {
                            var defaultVendor = await _context.Vendors.FirstOrDefaultAsync();
                            if (defaultVendor != null)
                            {
                                tenantVendorId = defaultVendor.VendorId;
                            }
                        }
                        
                        var newTenant = new Tenant
                        {
                            VendorId = tenantVendorId,
                            CompanyName = clientName + " (Individual)",
                            ContactPerson = clientName,
                            Email = clientEmail,
                            MobileNumber = clientMobile ?? "",
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("tenant123"), // Default password: tenant123
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "SuperAdmin"
                        };
                        _context.Tenants.Add(newTenant);
                    }
                }

                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Clients));
        }

        [HttpPost]
        public async Task<IActionResult> ReleaseClientFromBed(int roomId)
        {
            var room = await _context.PGRooms
                .Include(r => r.Flat)
                    .ThenInclude(f => f.Property)
                .FirstOrDefaultAsync(r => r.Id == roomId);

            if (room != null)
            {
                // Verify Vendor owns this property
                int? vendorId = GetVendorId();
                if (vendorId.HasValue && room.Flat?.Property?.VendorId != vendorId.Value)
                {
                    return Forbid();
                }

                room.IsOccupied = false;
                room.OccupiedByName = null;
                room.OccupiedByMobile = null;
                room.OccupiedByEmail = null;
                room.OccupiedByAadhar = null;
                room.OccupiedByEmergencyContact = null;
                room.OccupiedByAddress = null;
                room.OccupiedSince = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Clients));
        }
        #endregion

        #region Vendors
        [HttpGet]
        public async Task<IActionResult> Vendors()
        {
            if (User.IsInRole("Vendor"))
            {
                return Forbid();
            }
            var vendors = await _context.Vendors.ToListAsync();
            return View(vendors);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVendor(Vendor vendor, string password)
        {
            if (User.IsInRole("Vendor"))
            {
                return Forbid();
            }

            ModelState.Remove("PasswordHash");
            if (ModelState.IsValid)
            {
                var existing = await _context.Vendors.AnyAsync(v => v.Email == vendor.Email);
                if (existing)
                {
                    TempData["Error"] = "Email already in use.";
                    return RedirectToAction(nameof(Vendors));
                }

                vendor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                vendor.CreatedDate = System.DateTime.UtcNow;
                _context.Vendors.Add(vendor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Vendor created successfully.";
            }
            else
            {
                TempData["Error"] = "Invalid vendor data.";
            }
            return RedirectToAction(nameof(Vendors));
        }

        [HttpPost]
        public async Task<IActionResult> EditVendor(Vendor vendor, string? password)
        {
            if (User.IsInRole("Vendor"))
            {
                return Forbid();
            }

            ModelState.Remove("PasswordHash");
            if (ModelState.IsValid)
            {
                var existingVendor = await _context.Vendors.FindAsync(vendor.VendorId);
                if (existingVendor == null) return NotFound();

                var duplicateEmail = await _context.Vendors.AnyAsync(v => v.Email == vendor.Email && v.VendorId != vendor.VendorId);
                if (duplicateEmail)
                {
                    TempData["Error"] = "Email already in use.";
                    return RedirectToAction(nameof(Vendors));
                }

                existingVendor.CompanyName = vendor.CompanyName;
                existingVendor.ContactPerson = vendor.ContactPerson;
                existingVendor.Email = vendor.Email;
                existingVendor.MobileNumber = vendor.MobileNumber;
                existingVendor.IsActive = vendor.IsActive;
                existingVendor.ModifiedDate = System.DateTime.UtcNow;

                if (!string.IsNullOrEmpty(password))
                {
                    existingVendor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }

                _context.Vendors.Update(existingVendor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Vendor updated successfully.";
            }
            else
            {
                TempData["Error"] = "Invalid vendor data.";
            }
            return RedirectToAction(nameof(Vendors));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            if (User.IsInRole("Vendor"))
            {
                return Forbid();
            }

            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor != null)
            {
                _context.Vendors.Remove(vendor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Vendor deleted successfully.";
            }
            else
            {
                TempData["Error"] = "Vendor not found.";
            }
            return RedirectToAction(nameof(Vendors));
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
