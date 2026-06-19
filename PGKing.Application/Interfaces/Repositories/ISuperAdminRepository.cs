using System.Threading.Tasks;
using PGKing.Application.Entities;

namespace PGKing.Application.Interfaces.Repositories
{
    public interface ISuperAdminRepository : IRepository<SuperAdmin>
    {
        Task<SuperAdmin?> GetByUsernameAsync(string username);
    }
}
