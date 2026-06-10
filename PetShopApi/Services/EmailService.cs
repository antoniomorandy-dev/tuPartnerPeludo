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
                    var response = await httpClient.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                        return (1, "Correo enviado correctamente");
                    else
                        return (-1, "Error de Brevo: " + await response.Content.ReadAsStringAsync());
                }
                catch (Exception ex)
                {
                    return (-1, "Error de conexión: " + ex.Message);
                }
            }
        }
    }
}