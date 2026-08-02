using System.Collections.Generic;
using System.Threading.Tasks;
using PGKing.Application.Entities;

namespace PGKing.Application.Interfaces.Repositories
{
    public interface ITenantRepository : IRepository<Tenant>
    {
        Task<Tenant?> GetByEmailAsync(string email);
        Task<IEnumerable<Tenant>> GetByVendorIdAsync(int vendorId);
        new Task<IEnumerable<Tenant>> GetAllAsync();
    }
}
