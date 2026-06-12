using System.Buffers.Text;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MimeKit;

namespace PetShopApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost:5000";
        }
        public async Task<(int emailCodigo, string emailMensaje)> EnviarCorreoValidacion(string emailDestino, string nombre, string token)
        {
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST");
            var smtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? "587");
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER");
            var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS");
            var emailFrom = Environment.GetEnvironmentVariable("EMAIL_FROM");
            Console.WriteLine($"DEBUG: Host={smtpHost}, User={smtpUser}, Pass={smtpPass}, Email={emailFrom}");
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Tu Partner Peludo", emailFrom));
                message.To.Add(new MailboxAddress(nombre, emailDestino));
                message.Subject = "Activa tu cuenta de Partner Peludo";

                message.Body = new TextPart("html") {
                    Text = $"<p>Hola {nombre}, para completar tu registro haz clic aquí: <a href='{_baseUrl}/confirmar?token={token}'>Validar cuenta</a></p>"
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient()) 
                {
                    await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUser, smtpPass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return (1, "Correo enviado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error SMTP: {ex.Message}");
                return (-1, "Error SMTP: " + ex.Message);
            }
        }
    }
}