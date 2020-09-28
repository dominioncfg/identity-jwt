using System.Threading.Tasks;

namespace IdentityJWT.AuthServer.Services.Email
{
    public interface IEmailService
    {
        Task SendEmailAsync(string from, string to, string subject, string body);
    }
}
