using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Repositories;
using PGKing.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PGKing.Infrastructure.Repositories
{
    public class TenantRepository : Repository<Tenant>, ITenantRepository
    {
        public TenantRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Tenant?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            var cleanEmail = email.Trim().ToLower();

            return await _context.Tenants.FirstOrDefaultAsync(t => 
                t.Email.ToLower() == cleanEmail || 
                t.MobileNumber.Trim() == cleanEmail || 
                t.ContactPerson.ToLower() == cleanEmail);
        }

        public async Task<IEnumerable<Tenant>> GetByVendorIdAsync(int vendorId)
        {
            return await _context.Tenants.Include(t => t.Vendor).Where(t => t.VendorId == vendorId).ToListAsync();
        }

        public new async Task<IEnumerable<Tenant>> GetAllAsync()
        {
            return await _context.Tenants.Include(t => t.Vendor).ToListAsync();
        }
    }
}
