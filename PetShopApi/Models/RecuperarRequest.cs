namespace PetShopApi.Models
{
    public class RecuperarRequest
    {
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string Metodo { get; set; } // "WHATSAPP" o "EMAIL"
    }
}
