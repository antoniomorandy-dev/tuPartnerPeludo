namespace PetShopApi.Models
{
    public class SalidaMod
    {
        public int Codigo { get; set; }
        public string? Mensaje { get; set; }
        public List<MensajeRespuesta> Salidas { get; set; } = new List<MensajeRespuesta>();
    }
}
