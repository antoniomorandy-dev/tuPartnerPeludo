namespace PetShopApi.Mmodels
{
    public class Usuario
    {
        public int? UsuarioID { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Telefono { get; set; }
        public string? PasswordHash { get; set; }
        public string? CodigoValidacion { get; set; } // El número de 6 dígitos
        public bool? EstaValidado { get; set; } // Cambiará a true al validar
        public string? TokenRecuperacion { get; set; } // Para confirmar que el email es válido
        public DateTime? FechaExpiracionToken { get; set; }
        public int? IntentosFallidos { get; set; }
        public DateTime? FechaBloqueo { get; set; }
    }
}
