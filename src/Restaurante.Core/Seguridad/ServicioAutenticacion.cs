using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Restaurante.Core.Datos;

namespace Restaurante.Core.Seguridad;

/// <summary>Resultado de un inicio de sesión exitoso.</summary>
public record SesionUsuario(string Id, string Email, string Nombre, IReadOnlySet<string> Roles)
{
    public bool EsAdmin => Roles.Contains(Seguridad.Roles.Admin);
    public bool TieneRol(string rol) => Roles.Contains(rol);
}

/// <summary>
/// Valida credenciales contra las tablas de ASP.NET Core Identity usando el mismo
/// PasswordHasher que la web. Lo usa la aplicación de escritorio, así un usuario
/// creado en la web puede entrar al escritorio con la misma contraseña.
/// </summary>
public class ServicioAutenticacion
{
    private readonly RestauranteDbContext _db;
    private readonly IPasswordHasher<Usuario> _hasher = new PasswordHasher<Usuario>();

    public ServicioAutenticacion(RestauranteDbContext db) => _db = db;

    public async Task<SesionUsuario?> IniciarSesionAsync(string email, string contrasena)
    {
        var normalizado = email.Trim().ToUpperInvariant();
        var usuario = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizado);
        if (usuario?.PasswordHash is null) return null;

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, contrasena);
        if (resultado == PasswordVerificationResult.Failed) return null;

        var roles = await (from ur in _db.UserRoles
                           join r in _db.Roles on ur.RoleId equals r.Id
                           where ur.UserId == usuario.Id
                           select r.Name!).ToListAsync();

        // HashSet: preguntar "¿tiene el rol X?" es O(1) y no hay roles repetidos.
        return new SesionUsuario(usuario.Id, usuario.Email!, usuario.NombreCompleto, roles.ToHashSet());
    }
}
