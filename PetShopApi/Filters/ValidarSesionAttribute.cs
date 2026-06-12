using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PetShopApi.DAL;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

public class ValidarSesionAttribute : ActionFilterAttribute
{
    private readonly UsuarioDAL _usuarioDAL;
    public ValidarSesionAttribute(UsuarioDAL usuarioDAL)
    {
        _usuarioDAL = usuarioDAL;
    }
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
{
    // 1. Obtener la ruta una sola vez
    var path = context.HttpContext.Request.Path.Value?.ToLower() ?? string.Empty;

    // 2. Comprobar si la ruta es pública o si tiene el atributo [AllowAnonymous]
    bool esRutaPublica = path.Contains("/login") || 
                         path.Contains("/registrar") || 
                         path.Contains("/verificar-codigo") || 
                         path.Contains("/api/usuarios/confirmar");

    bool tieneAllowAnonymous = context.ActionDescriptor.EndpointMetadata.Any(m => m is AllowAnonymousAttribute);

    if (esRutaPublica || tieneAllowAnonymous)
    {
        await next();
        return; 
    }

    // 3. Validar encabezado Authorization para rutas protegidas
    var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();

    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
    {
        context.Result = new UnauthorizedResult();
        return;
    }

    string token = authHeader.Substring(7);

    // 4. Validar contra la base de datos
    bool esValido = await _usuarioDAL.ValidarTokenEnBD(token);

    if (!esValido)
    {
        context.Result = new UnauthorizedResult();
        return;
    }

    await next();
}
}