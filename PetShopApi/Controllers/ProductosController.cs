using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetShopApi.DAL;
using PetShopApi.Models;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ProductosController : ControllerBase
{
    private readonly ProductosDal _productosDAL;
    private readonly IWebHostEnvironment _env;
    public ProductosController(ProductosDal productosDAL, IWebHostEnvironment env)
    {
        _productosDAL = productosDAL;
        _env = env;
    }
    [HttpGet]
    public IActionResult Get()
    {
        var (salida, productos) = _productosDAL.ObtenerProductos();
        return Ok(new { salida, productos });
    }
    [HttpGet("listar")]
    public IActionResult ListarProductos()
    {
        var lista = _productosDAL.ObtenerProductos();
        return Ok(new
        {
            codigo = 1,
            productos = lista
        });
    }
    [HttpPost]
    public IActionResult Post([FromBody] ProductosMod producto)
    {
        var (salida, productoGuardado) = _productosDAL.GuardarProducto(producto);
        return Ok(new { salida, producto = productoGuardado });
    }
    [HttpPost("guardar")]
    public async Task<IActionResult> GuardarProducto([FromForm] ProductosMod producto)
    {
        if (Request.Form.Files == null || Request.Form.Files.Count == 0)
            return BadRequest("La imagen es requerida.");

        var archivo = Request.Form.Files[0];
        if (archivo == null || archivo.Length == 0)
            return BadRequest("La imagen es requerida.");

        string nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
        string rutaCarpeta = Path.Combine(_env.WebRootPath, "images", "productos");

        if (!Directory.Exists(rutaCarpeta)) Directory.CreateDirectory(rutaCarpeta);

        string rutaCompleta = Path.Combine(rutaCarpeta, nombreArchivo);

        using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream);
        }

        string urlGuardada = "/images/productos/" + nombreArchivo;

        var productos = new ProductosMod
        {
            Nombre = producto.Nombre,
            Precio = producto.Precio,
            UrlImagen = urlGuardada
        };

        (SalidaMod,ProductosMod) resultado = _productosDAL.GuardarProducto(productos);

        return Ok(new { codigo = resultado.Item1.Codigo, mensaje = resultado.Item1.Mensaje, Url = urlGuardada });
    }
}
