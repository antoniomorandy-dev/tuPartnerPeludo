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
    private readonly MetodosRecuperacionDal _metodosRecuperacionDal;
    private readonly EmailService _emailService;
    private readonly IWhatsappService _whatsappService;
    private readonly IConfiguration _configuration;

    public UsuariosController(UsuarioDAL usuarioDAL, MetodosRecuperacionDal metodosRecuperacionDal, EmailService emailService, IWhatsappService whatsappService, IConfiguration configuration)
    {
        _usuarioDAL = usuarioDAL;
        _metodosRecuperacionDal = metodosRecuperacionDal;
        _emailService = emailService;
        _whatsappService = whatsappService;
        _configuration = configuration;
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
            SalidaMod salida = new SalidaMod();

            (salida) = await _usuarioDAL.RegistrarUsuario(user, tokenEmail, codigoWS);
            Console.WriteLine("Mensaje de RegistrarUsuario: " + salida.Mensaje);

            if (salida.Codigo != 1)
            {
                await _usuarioDAL.EliminaRegistroUsuario(user);
                return BadRequest(salida);
            }
            if (salida.Codigo == 1)
            {
                //return Ok(new { regCodigo, regMensaje });
                salida = await _emailService.EnviarCorreoValidacion(user.Email, user.Nombre ?? "Usuario", tokenEmail);
                if (salida.Codigo == 1)
                {
                    Console.WriteLine("Envio Correo: " + salida.Mensaje);
                    return Ok(salida);
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
                    Console.WriteLine("Elimino registro: " + salida.Mensaje);
                    await _usuarioDAL.EliminaRegistroUsuario(user);
                    return StatusCode(500, salida);
                }
            }
            else
            {
                Console.WriteLine("Elimino registro: " + salida.Mensaje);
                await _usuarioDAL.EliminaRegistroUsuario(user);
                return BadRequest(salida);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new SalidaMod { Codigo = -1, Mensaje = "Error: " + ex.Message });
        }
    }
    [AllowAnonymous]
    [HttpGet("/api/Usuarios/confirmar")]
    public async Task<IActionResult> Confirmar([FromQuery] string token)
    {
        string? paginaIndex = _configuration["ConnectionStrings:PaginaIndex"];
        if (string.IsNullOrEmpty(token)) return BadRequest("Token no proporcionado.");
        if (string.IsNullOrEmpty(paginaIndex)) return StatusCode(500, "La URL de la página de inicio no está configurada.");
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
                            <a href='{paginaIndex}' class='btn'>Iniciar Sesión</a>
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
    [AllowAnonymous]
    public async Task<IActionResult> SolicitarRecuperacion([FromBody] RecuperarRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Metodo))
            return BadRequest(new { mensaje = "El método de recuperación es requerido." });

        string? paginaIndex = _configuration["ConnectionStrings:PaginaIndex"];
        if (string.IsNullOrEmpty(paginaIndex)) return StatusCode(500, "La URL de la página de inicio no está configurada.");
        Usuario? usuario = null;
        SalidaMod salida = new SalidaMod();

        if (request.Metodo == "WHATSAPP")
        {
            (usuario, salida) = await _usuarioDAL.ObtenerPorTelefono(request.Telefono ?? "", salida);
            if (usuario == null)
            {
                return Ok(new { salida });
            }
        }

        if (request.Metodo == "EMAIL")
        {
            (usuario, salida) = await _usuarioDAL.ObtenerPorEmail(request.Email ?? "", salida);
            if (usuario == null)
            {
                return Ok(new { salida });
            }
        }

        string token = Guid.NewGuid().ToString();
        if (usuario == null)
        {
            return Ok(new { salida });
        }
        salida = await _usuarioDAL.ActualizarTokenRecuperacion(usuario.UsuarioID ?? 0, token);

        if (salida.Codigo == 1)
        {
            if (request.Metodo == "WHATSAPP")
            {
                if (!string.IsNullOrWhiteSpace(usuario.Telefono))
                {
                    if (string.IsNullOrEmpty(paginaIndex)) return StatusCode(500, "La URL de la página de inicio no está configurada.");
                    string enlace = $"{paginaIndex}reset-password.html?token={token}";
                    await _whatsappService.EnviarMensajeAsync(usuario.Telefono, $"Hola {usuario.Nombre}, tu enlace: {enlace}");
                }
                else
                {
                    return Ok(new { codigo = 1, mensaje = "Si los datos existen, recibirás el enlace." });
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(usuario.Email))
                {
                    if (string.IsNullOrEmpty(paginaIndex)) return StatusCode(500, "La URL de la página de inicio no está configurada.");
                    string enlace = $"{paginaIndex}reset-password.html?token={token}";
                    string cuerpo = $"<h1>Recuperación de contraseña</h1><p>Hola {usuario.Nombre}, haz clic aquí: <a href='{enlace}'>Restablecer</a></p>";
                    salida = await _emailService.EnviarEmailAsync(usuario.Email, "Recupera tu contraseña", cuerpo);
                }
                else
                {
                    return Ok(salida);
                }
            }

            return Ok(new { codigo = 1, mensaje = "Enlace enviado con éxito." });
        }

        return Ok(salida);
    }
    [HttpPost("restablecer-final")]
    [AllowAnonymous]
    public async Task<IActionResult> RestablecerFinal([FromBody] RestablecerRequest request)
    {
        if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NuevaPassword))
        {
            return Ok(new { codigo = 0, mensaje = "Por favor, completa todos los campos." });
        }

        string passHash = BCrypt.Net.BCrypt.HashPassword(request.NuevaPassword);

        (SalidaMod salida, Usuario usuario) = _usuarioDAL.RestablecerPasswordFinal(request.Token, passHash);

        if (salida.Codigo == 1)
        {
            if (!string.IsNullOrEmpty(usuario.Email) && !string.IsNullOrEmpty(usuario.Nombre) && !string.IsNullOrEmpty(usuario.Apellido))
                await _emailService.EnviarCorreoPasswordActualizada(usuario.Email, usuario.Nombre, usuario.Apellido, request.Token);
            return Ok(new { salida });
        }
        else
        {
            return Ok(new { salida });
        }
    }
    [HttpGet("validar-token")]
    [AllowAnonymous]
    public IActionResult ValidarToken(string token)
    {
        return Ok(new { salida = _usuarioDAL.ValidarToken(token) });
    }
    [HttpGet("metodos-recuperacion")]
    [AllowAnonymous]
    public async Task<IActionResult> ObtenerMetodos()
    {
        try
        {
            List<MetodosRecuperacionMod> metodos = await Task.Run(() => _metodosRecuperacionDal.MetodosRecuperacion());
            var respuesta = new
            {
                salida = new
                {
                    codigo = 1,
                    metodos = metodos
                }
            };
            return Ok(respuesta);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { codigo = -1, mensaje = "Error al obtener métodos de recuperación: " + ex.Message });
        }
    }
}
