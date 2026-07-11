using System.Threading.Tasks;

namespace PGKing.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}
