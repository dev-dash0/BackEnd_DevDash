using DevDash.Services.IServices;
using System.Net.Mail;
using System.Net;
using DevDash.DTO;

namespace DevDash.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(EmailDto emailDto)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_config["EmailSettings:Port"]);
            var from = _config["EmailSettings:From"];
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:Password"];

            var smtpClient = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(from),
                Subject = emailDto.Subject,
                Body = emailDto.Body,
                IsBodyHtml = true
            };

            message.To.Add(emailDto.To);

            await smtpClient.SendMailAsync(message);
        }
    }
}
