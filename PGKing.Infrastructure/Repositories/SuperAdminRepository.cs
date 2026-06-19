using Microsoft.EntityFrameworkCore;
using PGKing.Application.Entities;
using PGKing.Application.Interfaces.Repositories;
using PGKing.Infrastructure.Data;
using System.Threading.Tasks;

namespace PGKing.Infrastructure.Repositories
{
    public class SuperAdminRepository : Repository<SuperAdmin>, ISuperAdminRepository
    {
        public SuperAdminRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<SuperAdmin?> GetByUsernameAsync(string username)
        {
            return await _context.SuperAdmins.FirstOrDefaultAsync(s => s.Username == username);
        }
    }
}
