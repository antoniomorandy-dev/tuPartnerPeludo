using Microsoft.Extensions.Options;
using PetShopApi.Models;

namespace PetShopApi.Services
{
    // Definimos un record para el resultado de EnviarCodigoValidacion
    public record CodigoValidacionResult(int Codigo, string Mensaje);

    public interface IWhatsappService
    {
        Task<CodigoValidacionResult> EnviarCodigoValidacion(string telefono, string codigo);
        Task<WhatsappResponse> EnviarMensajeAsync(string telefono, string mensaje);
    }

    public class WhatsappService : IWhatsappService
    {
        private readonly WhatsappSettings _settings;

        public WhatsappService(IOptions<WhatsappSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task<CodigoValidacionResult> EnviarCodigoValidacion(string telefono, string codigo)
        {
            try
            {
                using var client = new HttpClient();

                var token = _settings.Token;
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var payload = new { telefono, codigo };

                var url = $"{_settings.BaseUrl}/enviar-codigo";

                var response = await client.PostAsJsonAsync(url, payload);

                if (response.IsSuccessStatusCode)
                    return new CodigoValidacionResult(1, "Código enviado correctamente");
                else
                    return new CodigoValidacionResult(0, $"Error {response.StatusCode}");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"Excepción en EnviarCodigoValidacion: {ex.Message}");
                return new CodigoValidacionResult(-1, "No se pudo conectar con el servicio de WhatsApp. Intenta nuevamente.");
            }
        }

        public async Task<WhatsappResponse> EnviarMensajeAsync(string telefono, string mensaje)
        {
            using var client = new HttpClient();

            var payload = new
            {
                telefono,
                mensaje
            };

            try
            {
                var url = $"{_settings.BaseUrl}/enviar";

                var response = await client.PostAsJsonAsync(url, payload);

                return new WhatsappResponse
                {
                    Sent = response.IsSuccessStatusCode,
                    Message = response.IsSuccessStatusCode ? "Enviado" : "Error en servidor local"
                };
            }
            catch (Exception ex)
            {
                return new WhatsappResponse { Sent = false, Message = ex.Message };
            }
        }
    }
}
