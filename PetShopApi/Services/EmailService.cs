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
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _emailFrom;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = _configuration["EmailSettings:BaseUrl"] ?? "https://tupartnerpeludo.onrender.com";
            _smtpHost = _configuration["EmailSettings:SMTP_HOST"] ?? "smtp-relay.brevo.com";
            _smtpPort = int.Parse(_configuration["EmailSettings:SMTP_PORT"] ?? "2525");
            _smtpUser = _configuration["EmailSettings:SMTP_USER"] ?? "adb95a001@smtp-brevo.com";
            _smtpPass = _configuration["EmailSettings:SMTP_PASS"] ?? "xsmtpsib-b206b8390a395271d19d28fad8990400817f1b97a438ef7614e108a8049ee693-zm4tbqpjjCnjgN5g";
            _emailFrom = _configuration["EmailSettings:EMAIL_FROM"] ?? "partnerpeludo@gmail.com";
        }
        public async Task<SalidaMod> EnviarCorreoValidacion(string emailDestino, string nombre, string token)
        {
            if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass) || string.IsNullOrWhiteSpace(_emailFrom))
            {
                throw new InvalidOperationException("SMTP_HOST, SMTP_USER, SMTP_PASS o EMAIL_FROM no están configurados en las variables de entorno.");
            }
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Tu Partner Peludo", _emailFrom));
                message.To.Add(new MailboxAddress(nombre, emailDestino));
                message.Subject = "Activa tu cuenta de Partner Peludo";

                message.Body = new TextPart("html")
                {
                    Text = $"<p>Hola {nombre}, para completar tu registro haz clic aquí: <a href='{_baseUrl}/api/Usuarios/confirmar?token={token}'>Validar cuenta</a></p>"
                };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpUser, _smtpPass);
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
            if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser) ||
                string.IsNullOrWhiteSpace(_smtpPass) || string.IsNullOrWhiteSpace(_emailFrom))
            {
                return new SalidaMod { Codigo = -1, Mensaje = "Error de configuración SMTP." };
            }

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Tu Partner Peludo", _emailFrom));
                message.To.Add(new MailboxAddress("", emailDestino));
                message.Subject = asunto;
                message.Body = new TextPart("html") { Text = cuerpoHtml };

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpUser, _smtpPass);
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
        public async Task<SalidaMod> EnviarCorreoPasswordActualizada(string emailDestino, string nombre, string apellido, string token)
        {
            if (string.IsNullOrWhiteSpace(_smtpHost) || string.IsNullOrWhiteSpace(_smtpUser) || string.IsNullOrWhiteSpace(_smtpPass) || string.IsNullOrWhiteSpace(_emailFrom))
            {
                throw new InvalidOperationException("SMTP_HOST, SMTP_USER, SMTP_PASS o EMAIL_FROM no están configurados en las variables de entorno.");
            }
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Tu Partner Peludo", _emailFrom));
                message.To.Add(new MailboxAddress($"{nombre} {apellido}", emailDestino));
                message.Subject = "Se Actualizo su contraseña Tu Partner Peludo";
                message.Body = new TextPart("html")
                {
                    Text = $"<p>Hola {nombre} {apellido}, tu contraseña en \"Tu Partner Peludo\" ha sido actualizada correctamente.</p>"
                };
                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpUser, _smtpPass);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                return new SalidaMod { Codigo = 1, Mensaje = "Correo de restablecimiento enviado correctamente" };
            }
            catch (Exception ex)
            {
                return new SalidaMod { Codigo = -1, Mensaje = "Error SMTP: " + ex.Message };
            }
        }
    }
}