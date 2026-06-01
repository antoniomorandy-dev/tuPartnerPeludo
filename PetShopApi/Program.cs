using PetShopApi.DAL;
using PetShopApi.Models;
using PetShopApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers(options =>
{
    // Esto aplica el filtro a TODOS los controladores
    options.Filters.Add<ValidarSesionAttribute>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
});

builder.Services.AddScoped<ConexionFll>();
builder.Services.AddScoped<UsuarioDAL>();
builder.Services.AddScoped<EmailService>();
builder.Services.Configure<WhatsappSettings>(builder.Configuration.GetSection("WhatsappSettings"));
builder.Services.AddScoped<IWhatsappService, WhatsappService>();

var app = builder.Build();

app.UseCors("PublicPolicy");
//app.UseAuthorization();
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PetShop API V1");
    c.RoutePrefix = string.Empty;
});

if (app.Environment.IsDevelopment())
{
    
}

app.MapControllers();

app.Run();