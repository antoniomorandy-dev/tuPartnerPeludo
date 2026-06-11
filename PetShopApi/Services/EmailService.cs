using System.Buffers.Text;
using System.Net;
using System.Net.Mail;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PetShopApi.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = _configuration["EmailSettings:BaseUrl"];
        }
        public async Task<(int emailCodigo, string emailMensaje)> EnviarCorreoValidacion(string emailDestino, string nombre, string token)
        {
            var url = "https://api.brevo.com/v3/smtp/email";
            
            // 1. Asegúrate de que esta variable en Render tenga la clave xkeysib-...
            var apiKey = _configuration["EmailSettings:SenderPassword"];

            // 2. Asegúrate de que esta variable en Render sea el correo verificado de tu panel
            var senderEmail = _configuration["EmailSettings:SenderEmail"];

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("api-key", apiKey);

                var payload = new
                {
                    sender = new { name = "Tu Partner Peludo", email = senderEmail },
                    to = new[] { new { email = emailDestino, name = nombre } },
                    subject = "Activa tu cuenta de Partner Peludo",
                    htmlContent = $"<p>Hola {nombre}, para completar tu registro haz clic aquí: <a href='{_baseUrl}/confirmar?token={token}'>Validar cuenta</a></p>"
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try 
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
                Console.WriteLine($"Error enviando correo: {ex.Message}");
                return (-1, $"No se pudo enviar el correo de validación. Intenta nuevamente. {ex.Message} ");
            }
        }
    }
}