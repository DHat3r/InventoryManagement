using BCrypt.Net;
using InventoryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Api.Services;

/// <summary>
/// Configuración para seed de usuario admin
/// </summary>
public class AdminSeedOptions
{
    public string UserName { get; set; } = "admin";
    public string Rut { get; set; } = "765432105";
    public string FullName { get; set; } = "Administrador del Sistema";
    public string Password { get; set; } = "Admin123!";
    public bool RotateOnStartup { get; set; } = true;
    public bool RunOnStartup { get; set; } = true;
}

/// <summary>
/// Servicio para asegurar que el usuario admin exista al arrancar la app
/// </summary>
public interface IAdminSeedService
{
    Task EnsureAdminExistsAsync(CancellationToken ct = default);
}

public class AdminSeedService : IAdminSeedService
{
    private readonly InventoryDbContext _db;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        InventoryDbContext db,
        IOptions<AdminSeedOptions> options,
        ILogger<AdminSeedService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EnsureAdminExistsAsync(CancellationToken ct = default)
    {
        if (!_options.RunOnStartup)
        {
            _logger.LogInformation("⏭️ Admin seed deshabilitado por configuración");
            return;
        }

        var admin = await _db.Users
            .FirstOrDefaultAsync(u => u.UserName == _options.UserName, ct);

        var newHash = HashPassword(_options.Password);

        if (admin is null)
        {
            // Crear admin
            admin = new User
            {
                UserName = _options.UserName,
                Rut = NormalizeRut(_options.Rut),
                PasswordHash = newHash,
                FullName = _options.FullName,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(admin);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("✅ Usuario admin creado: {UserName} (RUT: {Rut})",
                admin.UserName, FormatRut(admin.Rut));
        }
        else if (_options.RotateOnStartup && admin.PasswordHash != newHash)
        {
            // Rotar password si cambió en config
            var oldHashPreview = admin.PasswordHash[..Math.Min(10, admin.PasswordHash.Length)];
            admin.PasswordHash = newHash;
            admin.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogWarning("🔐 Password admin rotado: {UserName} (hash anterior: {OldHash}...)",
                admin.UserName, oldHashPreview);
        }
        else
        {
            _logger.LogInformation("✅ Usuario admin ya existe: {UserName}", admin.UserName);
        }
    }

    private string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    private static string NormalizeRut(string? rut)
        => string.IsNullOrWhiteSpace(rut) ? null! : rut.Replace(".", "").Replace("-", "").ToUpper();

    private static string FormatRut(string? rut)
    {
        if (string.IsNullOrEmpty(rut) || rut.Length < 2) return rut ?? "";
        var body = rut[..^1];
        var dv = rut[^1];
        return $"{long.Parse(body):##.###.###}-{dv}";
    }
}