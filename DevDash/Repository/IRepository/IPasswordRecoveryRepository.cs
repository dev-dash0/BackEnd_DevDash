using DevDash.DTO.Account;

namespace DevDash.Repository.IRepository
{
    public interface IPasswordRecoveryRepository
    {
        Task SendEmailAsync(EmailBodyDTO email);
    }
}
