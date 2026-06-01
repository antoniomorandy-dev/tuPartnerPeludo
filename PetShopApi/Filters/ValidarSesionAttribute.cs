using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PetShopApi.DAL;
using System.Threading.Tasks;

public class ValidarSesionAttribute : ActionFilterAttribute
{
    private readonly UsuarioDAL _usuarioDAL;
    public ValidarSesionAttribute(UsuarioDAL usuarioDAL)
    {
        _usuarioDAL = usuarioDAL;
    }
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLower() ?? string.Empty;

        // EXCLUIR RUTAS PÚBLICAS
        if (path.Contains("/login") || path.Contains("/registrar") || path.Contains("/verificar-codigo"))
        {
            await next();
            return; // Permite que continúe sin validar nada
        }
        // 1. Obtener el encabezado Authorization
        var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

        // 2. Validar formato (Bearer <token>)
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Result = new UnauthorizedResult(); // 401
            return;
        }

        string token = authHeader.Substring(7);

        // 3. Validar contra tu base de datos (Tu DAL)
        // Aquí llamas a tu método que busca el token en la tabla Sesiones
        bool esValido = await _usuarioDAL.ValidarTokenEnBD(token);

        if (!esValido)
        {
            context.Result = new UnauthorizedResult(); // 401 si el token no existe o expiró
            return;
        }

        await next();
    }
}