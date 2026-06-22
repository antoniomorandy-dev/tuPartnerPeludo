using PetShopApi.Models;
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
            _baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5000";
        }
        public async Task<SalidaMod> EnviarCorreoValidacion(string emailDestino, string nombre, string token)
        {
            var smtpHost = _configuration["EmailSettings:SMTP_HOST"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SMTP_PORT"] ?? "587");
            var smtpUser = _configuration["EmailSettings:SMTP_USER"];
            var smtpPass = _configuration["EmailSettings:SMTP_PASS"];
            var emailFrom = _configuration["EmailSettings:EMAIL_FROM"];

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPass) || string.IsNullOrWhiteSpace(emailFrom))
            {
                throw new InvalidOperationException("SMTP_HOST, SMTP_USER, SMTP_PASS o EMAIL_FROM no están configurados en las variables de entorno.");
            }
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Tu Partner Peludo", emailFrom));
                message.To.Add(new MailboxAddress(nombre, emailDestino));
                message.Subject = "Activa tu cuenta de Partner Peludo";

                message.Body = new TextPart("html") {
                    Text = $"<p>Hola {nombre}, para completar tu registro haz clic aquí: <a href='{_baseUrl}/api/Usuarios/confirmar?token={token}'>Validar cuenta</a></p>"
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient()) 
                {
                    await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUser, smtpPass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return new SalidaMod { Codigo = 1, Mensaje = "Correo enviado correctamente" };
            }
            catch (Exception ex)
            {
                return new SalidaMod { Codigo = -1, Mensaje = "Error SMTP: " + ex.Message };
            }
        }
        public async Task<SalidaMod> EnviarEmailAsync(string emailDestino, string asunto, string cuerpoHtml)
        {
            var smtpHost = _configuration["EmailSettings:SMTP_HOST"];
            var smtpPort = int.Parse(_configuration["EmailSettings:SMTP_PORT"] ?? "587");
            var smtpUser = _configuration["EmailSettings:SMTP_USER"];
            var smtpPass = _configuration["EmailSettings:SMTP_PASS"];
            var emailFrom = _configuration["EmailSettings:EMAIL_FROM"];

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser) ||
                string.IsNullOrWhiteSpace(smtpPass) || string.IsNullOrWhiteSpace(emailFrom))
            {
                return new SalidaMod { Codigo = -1, Mensaje = "Error de configuración SMTP." };
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Tu Partner Peludo", emailFrom));
                message.To.Add(new MailboxAddress("", emailDestino));
                message.Subject = asunto;
                message.Body = new TextPart("html") { Text = cuerpoHtml };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(smtpUser, smtpPass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                return new SalidaMod { Codigo = 1, Mensaje = "Correo enviado correctamente." };
            }
            catch (Exception ex)
            {
                return new SalidaMod { Codigo = -1, Mensaje = "Error al enviar correo: " + ex.Message };
            }
        }
    }
}