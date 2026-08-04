using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EcoTrack.Infrastructure
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task EnvoyerAsync(string destinataire, string sujet, string corpsHtml)
        {
            var smtpSection = _configuration.GetSection("Smtp");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                smtpSection["FromName"] ?? "EcoTrack - Ecobank Togo",
                smtpSection["FromAddress"] ?? "noreply@ecobank.tg"));
            message.To.Add(MailboxAddress.Parse(destinataire));
            message.Subject = sujet;
            message.Body = new TextPart("html") { Text = corpsHtml };

            using var client = new SmtpClient();

            await client.ConnectAsync(
                smtpSection["Host"],
                int.Parse(smtpSection["Port"] ?? "587"),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                smtpSection["Username"],
                smtpSection["Password"]);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}