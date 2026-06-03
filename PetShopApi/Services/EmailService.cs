using System.Buffers.Text;
using System.Net;
using System.Net.Mail;

namespace PetShopApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<(int codigo, string mensaje)> EnviarCorreoValidacion(string emailDestino, string token)
        {
            try
            {
                int codigo = 0;
                string mensaje = string.Empty;

                var settings = _config.GetSection("EmailSettings");

                var baseUrl = settings["BaseUrl"];

                string enlace = $"{baseUrl}api/usuarios/confirmar?token={token}";

                var smtpPortString = settings["SmtpPort"];
                if (string.IsNullOrWhiteSpace(smtpPortString))
                {
                    throw new InvalidOperationException("El puerto SMTP no está configurado.");
                }

                var smtpServer = settings["SmtpServer"]; if (string.IsNullOrWhiteSpace(smtpServer))
                    if (string.IsNullOrWhiteSpace(smtpServer))
                    {
                        throw new InvalidOperationException("El servidor SMTP no está configurado.");
                    }

                var client = new SmtpClient(smtpServer, int.Parse(smtpPortString))
                {
                    Credentials = new NetworkCredential(settings["SenderEmail"], settings["SenderPassword"]),
                    EnableSsl = true,
                    UseDefaultCredentials = false, // ¡Muy importante!
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    TargetName = "STARTTLS/smtp.gmail.com"
                };

                var senderEmail = settings["SenderEmail"];
                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    throw new InvalidOperationException("La dirección de correo del remitente no está configurada.");
                }

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "Partner Peludo"),
                    Subject = "Activa tu cuenta de Partner Peludo",
                    Body = $@"
                    <h2>¡Bienvenido a Tu Partner Peludo!</h2>
                    <p>Para completar tu registro, haz clic en el siguiente botón:</p>
                    <a href='{enlace}' style='padding: 10px 20px; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px;'>Validar mi cuenta</a>
                    <p>Si el botón no funciona, copia y pega este enlace: <br> {enlace}</p>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(emailDestino);
                //client.Send(mailMessage);
                await client.SendMailAsync(mailMessage);
                codigo = 1;
                mensaje = "Correo de validación enviado correctamente.";
                return (codigo, mensaje);
            }
            catch (Exception ex)
            {
                string detalle = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Console.WriteLine($"Error detallado enviando correo: {detalle}"); // Esto saldrá en los logs de Render
                return (-1, "Error: " + detalle); 
            }
        }
    }
}