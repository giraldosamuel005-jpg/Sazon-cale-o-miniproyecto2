// ======================================================================
// Sazón Caleño · Sistema de órdenes para restaurante
// Miniproyecto 2 · Lenguajes de Programación · Universidad Santiago de Cali · 2026
// Autores: Ángel David Gómez Gómez · Samuel Darío Giraldo Paz
// ======================================================================

using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Datos;
using Restaurante.Core.Seguridad;
using Restaurante.Core.Servicios;
using Restaurante.Web.Infraestructura;

var builder = WebApplication.CreateBuilder(args);

// ---------- Base de datos (EF Core, compartida con el escritorio) ----------
// PostgreSQL en la nube si hay cadena (ConnectionStrings:Restaurante o variable SAZON_DB);
// si no, SQLite local en %LOCALAPPDATA%\RestauranteApp.
var cadena = ConexionBaseDatos.Resolver(builder.Configuration.GetConnectionString("Restaurante"));
builder.Services.AddDbContext<RestauranteDbContext>(o => ConexionBaseDatos.Configurar(o, cadena));

// Llaves de la cookie de sesión guardadas en la base de datos (sobreviven reinicios del servidor).
builder.Services.AddDataProtection()
    .SetApplicationName("SazonCaleno")
    .PersistKeysToDbContext<RestauranteDbContext>();

// ---------- ASP.NET Core Identity con roles ----------
builder.Services
    .AddIdentity<Usuario, IdentityRole>(o =>
    {
        o.Password.RequiredLength = 6;
        o.User.RequireUniqueEmail = true;
        o.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<RestauranteDbContext>()
    .AddClaimsPrincipalFactory<ClaimsUsuarioFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Cuenta/Login";
    o.LogoutPath = "/Cuenta/Logout";
    o.AccessDeniedPath = "/Cuenta/AccesoDenegado";
    o.ExpireTimeSpan = TimeSpan.FromHours(10); // Un turno de trabajo.
});

// ---------- Servicios de negocio de la biblioteca Restaurante.Core ----------
builder.Services.AddScoped<ServicioMenu>();
builder.Services.AddScoped<ServicioMesas>();
builder.Services.AddScoped<ServicioOrdenes>();
builder.Services.AddScoped<ServicioFacturacion>();
builder.Services.AddScoped<ServicioReportes>();

// ---------- MVC: toda la aplicación exige sesión salvo lo marcado [AllowAnonymous] ----------
builder.Services.AddControllersWithViews(o =>
    o.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())));

var app = builder.Build();

// Crea/actualiza la base de datos y carga los datos iniciales.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RestauranteDbContext>();
    await InicializadorDatos.InicializarAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Cultura colombiana: fechas dd/MM/yyyy y moneda en pesos.
var colombia = new CultureInfo("es-CO");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(colombia),
    SupportedCultures = new[] { colombia },
    SupportedUICultures = new[] { colombia }
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
