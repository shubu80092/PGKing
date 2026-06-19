using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Repositories;
using PGKing.Infrastructure.Data;
using System.Threading.Tasks;

namespace PGKing.Infrastructure.Repositories
{
    public class VendorRepository : Repository<Vendor>, IVendorRepository
    {
        public VendorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<Vendor?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            var cleanEmail = email.Trim().ToLower();

            return await _context.Vendors.FirstOrDefaultAsync(v => 
                v.Email.ToLower() == cleanEmail || 
                v.MobileNumber.Trim() == cleanEmail || 
                v.ContactPerson.ToLower() == cleanEmail);
        }
    }
}
