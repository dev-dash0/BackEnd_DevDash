using DevDash.DTO;

namespace DevDash.Services.IServices
{
    public interface IEmailService
    {
        Task SendAsync(EmailDto emailDto);
    }
}