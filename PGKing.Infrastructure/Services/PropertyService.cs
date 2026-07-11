using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PGKing.Application.DTOs;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Repositories;
using PGKing.Application.Interfaces.Services;
using PGKing.Infrastructure.Data;

namespace PGKing.Infrastructure.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly IRepository<Property> _propertyRepository;
        private readonly ApplicationDbContext _context; // For advanced includes

        public PropertyService(IRepository<Property> propertyRepository, ApplicationDbContext context)
        {
            _propertyRepository = propertyRepository;
            _context = context;
        }

        public async Task<IEnumerable<Property>> GetAllPropertiesAsync()
        {
            return await _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media)
                .ToListAsync();
        }

        public async Task<Property?> GetPropertyByIdAsync(int id)
        {
            return await _context.Properties
                .Include(p => p.City)
                .Include(p => p.State)
                .Include(p => p.Media)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Rooms)
                .Include(p => p.Flats)
                    .ThenInclude(f => f.Media)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Property> CreatePropertyAsync(CreatePropertyRequest request, int? vendorId = null)
        {
            var property = new Property
            {
                Title = request.Title,
                Address = request.Address,
                StateId = request.StateId,
                CityId = request.CityId,
                Description = request.Description,
                Amenities = request.Amenities,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CreatedAt = DateTime.Now,
                VendorId = vendorId
            };

            return await _propertyRepository.AddAsync(property);
        }

        public async Task UpdatePropertyAsync(int id, CreatePropertyRequest request)
        {
            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null) throw new Exception("Property not found");

            property.Title = request.Title;
            property.Address = request.Address;
            property.StateId = request.StateId;
            property.CityId = request.CityId;
            property.Description = request.Description;
            property.Amenities = request.Amenities;
            property.Latitude = request.Latitude;
            property.Longitude = request.Longitude;

            await _propertyRepository.UpdateAsync(property);
        }

        public async Task DeletePropertyAsync(int id)
        {
            var property = await _propertyRepository.GetByIdAsync(id);
            if (property == null) throw new Exception("Property not found");

            await _propertyRepository.DeleteAsync(property);
        }
    }
}
