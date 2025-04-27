using DevDash.DTO.Account;
using DevDash.Repository.IRepository;
using MailKit.Net.Smtp;
using MimeKit;

namespace DevDash.Repository
{
    public class PasswordRecoveryRepository : IPasswordRecoveryRepository
    {
        private readonly IConfiguration _configuration;

        public PasswordRecoveryRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(EmailBodyDTO email)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("DevDash", emailSettings["From"]));
            message.To.Add(new MailboxAddress("", email.To));
            message.Subject = email.Subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = email.Body };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["Port"]), false);
            await client.AuthenticateAsync(emailSettings["Username"], emailSettings["Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
