using Microsoft.AspNetCore.Mvc;
using PetShopApi.DAL;
using PetShopApi.Mmodels;
using PetShopApi.Models;
using PetShopApi.Services;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioDAL _usuarioDAL;
    private readonly EmailService _emailService;
    private readonly IWhatsappService _whatsappService;

    public UsuariosController(UsuarioDAL usuarioDAL, EmailService emailService, IWhatsappService whatsappService)
    {
        _usuarioDAL = usuarioDAL;
        _emailService = emailService;
        _whatsappService = whatsappService;
    }

    [HttpPost("registrar")]
    public async Task<IActionResult> Registrar([FromBody] Usuario user)
    {
        Console.WriteLine("Datos recibidos: " + user.ToString());
        try
        {
            if (user == null) return BadRequest(new { codigo = 0, mensaje = "Datos inválidos" });
            if (string.IsNullOrWhiteSpace(user.Email)) return BadRequest(new { codigo = 0, mensaje = "Email obligatorio" });
            if (string.IsNullOrWhiteSpace(user.Telefono)) return BadRequest(new { codigo = 0, mensaje = "Teléfono obligatorio" });

            string codigoWS = new Random().Next(100000, 999999).ToString();
            string tokenEmail = Guid.NewGuid().ToString();

            var (regCodigo, regMensaje) = await _usuarioDAL.RegistrarUsuario(user, tokenEmail, codigoWS);
            Console.WriteLine("Mensaje de RegistrarUsuario: " + regMensaje);

            if (regCodigo != 1)
            {
                await _usuarioDAL.EliminaRegistroUsuario(user);
                return BadRequest(new SalidaMod { Codigo = regCodigo, Mensaje = regMensaje });
            }
            if (regCodigo == 1)
            {
                //return Ok(new { regCodigo, regMensaje });
                var (emailCodigo, emailMensaje) = await _emailService.EnviarCorreoValidacion(user.Email, user.Nombre ?? "Usuario", tokenEmail);
                if (emailCodigo == 1) 
                {
                    Console.WriteLine("Envio Correo: " + emailMensaje);
                    return Ok(new SalidaMod { Codigo = emailCodigo, Mensaje = emailMensaje });
                }
                //var (wsCodigo, wsMensaje) = await _whatsappService.EnviarCodigoValidacion(user.Telefono, codigoWS);
                //if (emailCodigo == 1 && wsCodigo == 1)
                //{
                //    return Ok(new { regCodigo, regMensaje });
                //}
                //else if (wsCodigo != 1)
                //{
                //    return Ok(new { wsCodigo, wsMensaje });
                //}
                else 
                {
                    Console.WriteLine("Elimino registro: " + regMensaje);
                    await _usuarioDAL.EliminaRegistroUsuario(user);
                    return StatusCode(500, new SalidaMod { codigo = emailCodigo, mensaje = $"Error desconocido en la validación. {emailMensaje}" } );
                }
                
            }
            else
            {
                Console.WriteLine("Elimino registro: " + regMensaje);
                await _usuarioDAL.EliminaRegistroUsuario(user);
                return BadRequest(new SalidaMod { codigo = regCodigo, mensaje = regMensaje });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SalidaMod { codigo = -1, mensaje = "Error: " + ex.Message });
        }
    }
    [AllowAnonymous]
    [HttpGet("/api/Usuarios/confirmar")]
    public async Task<IActionResult> Confirmar([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token)) return BadRequest("Token no proporcionado.");
        try
        {
            bool confirmado = await _usuarioDAL.ConfirmarEmail(token);
        if (confirmado)
            {
                string htmlResponse = $@"
                <html>
                    <head>
                        <meta charset='UTF-8'> 
                        <style>
                            body {{ font-family: 'Segoe UI', sans-serif; display: flex; justify-content: center; align-items: center; height: 100vh; background-color: #f4f7f6; }}
                            .card {{ background: white; padding: 2rem; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); text-align: center; }}
                            .btn {{ background-color: #28a745; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold; display: inline-block; margin-top: 20px; }}
                            h1 {{ color: #333; }}
                        </style>
                    </head>
                    <body>
                        <div class='card'>
                            <h1>¡Cuenta activada con éxito!</h1>
                            <p>Tu correo ha sido verificado. Ya eres parte de la comunidad de <b>PetShop</b>.</p>
                            <a href='https://amorandy.github.io/tuPartnerPeludo/index.html' class='btn'>Iniciar Sesión</a>
                        </div>
                    </body>
                </html>";
                return Content(htmlResponse, "text/html");
            }
            else
            {
                return BadRequest("El enlace es inválido o ya ha expirado.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error al confirmar: {ex.Message}");
        }
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            string token = Guid.NewGuid().ToString();
            var (usuario, salida, tokenn) = await _usuarioDAL.Login(request.Email ?? "", request.Password ?? "", token);
            if (salida.Codigo == -1)
            {
                return StatusCode(500, salida);
            }

            return Ok(new
            {
                codigo = salida.Codigo,
                mensaje = salida.Mensaje,
                user = usuario?.Nombre,
                token = tokenn
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { codigo = -1, mensaje = "Error crítico: " + ex.Message });
        }
    }
    [HttpPost("verificar-codigo")]
    public async Task<IActionResult> VerificarCodigo([FromBody] dynamic data)
    {
        string email = data.GetProperty("email").GetString();
        string codigo = data.GetProperty("codigo").GetString();

        var esValido = await _usuarioDAL.ValidarCodigoWhatsApp(email, codigo);

        if (esValido)
        {
            return Ok(new { codigo = 1, mensaje = "Validado" });
        }

        return BadRequest(new { codigo = 0, mensaje = "Código inválido" });
    }
    [HttpPost("solicitar-recuperacion")]
    public async Task<IActionResult> SolicitarRecuperacion([FromBody] RecuperarRequest request)
    {
        // 1. Validar entrada
        if (request == null || string.IsNullOrWhiteSpace(request.Telefono))
        {
            return Ok(new { codigo = 0, mensaje = "Si el número existe, recibirás un mensaje." });
        }

        // 2. Usar SalidaMod internamente
        SalidaMod salida = new SalidaMod();
        var usuario = await _usuarioDAL.ObtenerPorTelefono(request.Telefono, salida);

        if (usuario == null)
        {
            return Ok(new { codigo = 0, mensaje = "Si el número existe, recibirás un mensaje." });
        }

        if (string.IsNullOrEmpty(usuario.Telefono))
        {
            return Ok(new { codigo = 0, mensaje = "Si el número existe, recibirás un mensaje." });
        }

        // 3. Generar Token y actualizar
        string token = Guid.NewGuid().ToString();
        usuario.TokenRecuperacion = token;
        usuario.FechaExpiracionToken = DateTime.Now.AddMinutes(15);

        // NOTA: Usa el método de tu DAL para guardar, ya que SaveChanges() no existe en tu clase
        bool guardado = await _usuarioDAL.ActualizarTokenRecuperacion(usuario.UsuarioID, token);

        if (guardado)
        {
            string enlace = $"https://amorandy.github.io/reset-password.html?token={token}";
            string mensajeWs = $"Hola {usuario.Nombre}, haz clic aquí para restablecer tu contraseña: {enlace}";
            await _whatsappService.EnviarMensajeAsync(usuario.Telefono, mensajeWs);
            return Ok(new { codigo = 1, mensaje = "Enlace enviado con éxito." });
        }

        return Ok(new { codigo = -1, mensaje = "Error al procesar la solicitud." });
    }
    [HttpPost("restablecer-final")]
    public async Task<IActionResult> RestablecerFinal([FromBody] RestablecerRequest request)
    {
        // 1. Validaciones de entrada
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NuevaPassword))
        {
            return Ok(new { codigo = 0, mensaje = "Por favor, completa todos los campos." });
        }

        // 2. Encriptar usando BCrypt (Igual que en tu método RegistrarUsuario)
        //
        string passHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);

        // 3. Llamar a la DAL para actualizar en la base de datos
        bool exito = await _usuarioDAL.RestablecerPasswordFinal(request.Token, passHash);

        if (exito)
        {
            return Ok(new { codigo = 1, mensaje = "¡Contraseña actualizada con éxito!" });
        }
        else
        {
            return Ok(new { codigo = -1, mensaje = "El enlace es inválido o ya ha expirado." });
        }
    }
}
