using System.Threading.Tasks;
using PGKing.Application.Entities;

namespace PGKing.Application.Interfaces.Repositories
{
    public interface IVendorRepository : IRepository<Vendor>
    {
        Task<Vendor?> GetByEmailAsync(string email);
    }
}
