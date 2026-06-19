using System.Collections.Generic;
using System.Threading.Tasks;
using PGKing.Application.DTOs;
using PGKing.Application.Entities;

namespace PGKing.Application.Interfaces.Services
{
    public interface IPropertyService
    {
        Task<IEnumerable<Property>> GetAllPropertiesAsync();
        Task<Property?> GetPropertyByIdAsync(int id);
        Task<Property> CreatePropertyAsync(CreatePropertyRequest request, int? vendorId = null);
        Task UpdatePropertyAsync(int id, CreatePropertyRequest request);
        Task DeletePropertyAsync(int id);
    }
}
