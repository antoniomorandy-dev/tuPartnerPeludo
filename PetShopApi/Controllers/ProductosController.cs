using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetShopApi.DAL;
using PetShopApi.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http.HttpResults;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ProductosController : ControllerBase
{
    private readonly ProductosDal _productosDAL;
    private readonly IWebHostEnvironment _env;
    private readonly Cloudinary _cloudinary;
    private readonly IConfiguration _configuration;
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    public ProductosController(ProductosDal productosDAL, IWebHostEnvironment env, IConfiguration configuration)
    {
        _productosDAL = productosDAL;
        _env = env;
        _configuration = configuration;
        _cloudName = _configuration["CLOUDINARY_CLOUD_NAME"] ?? "";
        _apiKey = _configuration["CLOUDINARY_API_KEY"] ?? "";
        _apiSecret = _configuration["CLOUDINARY_API_SECRET"] ?? "";

        Account account = new Account(_cloudName, _apiKey, _apiSecret);
        _cloudinary = new Cloudinary(account);
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
        var (salida, lista) = _productosDAL.ObtenerProductos();
        return Ok(new { salida, productos = lista });
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
        try
        {
            var archivo = Request.Form.Files[0];
            if (archivo == null || archivo.Length == 0)
                return Ok(new { codigo = 0, mensaje = "Se requiere la Imagen", archivo });

            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(archivo.FileName, archivo.OpenReadStream()),
                Folder = "productos"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            string urlGuardada = uploadResult.SecureUrl.ToString();

            var productoNuevo = new ProductosMod
            {
                Nombre = producto.Nombre,
                Precio = producto.Precio,
                Descripcion = producto.Descripcion,
                UrlImagen = urlGuardada
            };

            _productosDAL.GuardarProducto(productoNuevo);

            return Ok(new { codigo = 1, mensaje = "Producto guardado con éxito", url = urlGuardada });
        }
        catch (Exception ex)
        {
            return Ok(new { codigo = -1, mensaje = ex.Message });
        }
    }
}
