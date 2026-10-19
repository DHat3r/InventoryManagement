using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.Api.Services;

/// <summary>
/// Servicio de autenticación (JWT + Refresh tokens + BCrypt)
/// </summary>
public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<RefreshResponseDto?> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<UserInfoDto?> GetUserInfoAsync(int userId, CancellationToken ct = default);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashToken(string token);
}

public class AuthService : IAuthService
{
    private readonly InventoryDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    // Configuración JWT
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _accessTokenMinutes;
    private readonly int _refreshTokenDays;

    public AuthService(
        InventoryDbContext db,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;

        _jwtSecret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret no configurado");
        _jwtIssuer = config["Jwt:Issuer"] ?? "InventoryManagement.Api";
        _jwtAudience = config["Jwt:Audience"] ?? "InventoryManagement.Client";
        _accessTokenMinutes = config.GetValue<int>("Jwt:AccessTokenMinutes", 15);
        _refreshTokenDays = config.GetValue<int>("Jwt:RefreshTokenDays", 7);
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserName == dto.UserName && u.IsActive, ct);

        if (user is null || !VerifyPassword(dto.Password, user.PasswordHash))
        {
            // Incrementar intentos fallidos (si existe usuario)
            if (user is not null)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                    _logger.LogWarning("🔒 Usuario bloqueado por 5 intentos fallidos: {UserName}", user.UserName);
                }
                await _db.SaveChangesAsync(ct);
            }
            return null;
        }

        // Verificar si está bloqueado
        if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            _logger.LogWarning("🔒 Intento de login en cuenta bloqueada: {UserName}", user.UserName);
            return null;
        }

        // Login exitoso: resetear intentos fallidos
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;

        // Generar tokens
        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        var refreshTokenHash = HashToken(refreshToken);

        // Guardar refresh token
        var rt = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(rt);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("✅ Login exitoso: {UserName} (Role: {Role})", user.UserName, user.Role);

        return new LoginResponseDto(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            User: new UserInfoDto(
                user.Id, user.UserName, user.Rut, user.FullName, user.Role, user.IsActive,
                user.CreatedAt, user.LastLoginAt
            )
        );
    }

    public async Task<RefreshResponseDto?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);

        var storedToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (storedToken is null || !storedToken.IsActive || storedToken.User is null || !storedToken.User.IsActive)
        {
            _logger.LogWarning("🔄 Refresh token inválido, expirado o revocado");
            return null;
        }

        // Verificar reuso (rotación): si ya fue reemplazado, revocar toda la cadena
        if (storedToken.ReplacedByTokenHash != null)
        {
            _logger.LogWarning("🚨 Posible robo de refresh token detectado para usuario: {UserId}", storedToken.UserId);
            
            // Revocar toda la cadena de tokens del usuario
            var userTokens = await _db.RefreshTokens
                .Where(rt => rt.UserId == storedToken.UserId && rt.RevokedAt == null)
                .ToListAsync(ct);
            
            foreach (var t in userTokens)
            {
                t.RevokedAt = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
            return null;
        }

        // Rotación: revocar token actual y crear uno nuevo
        storedToken.RevokedAt = DateTime.UtcNow;

        var newAccessToken = GenerateAccessToken(storedToken.User);
        var newRefreshToken = GenerateRefreshToken();
        var newRefreshTokenHash = HashToken(newRefreshToken);

        var newRt = new RefreshToken
        {
            UserId = storedToken.UserId,
            TokenHash = newRefreshTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays),
            ReplacedByTokenHash = newRefreshTokenHash,
            CreatedAt = DateTime.UtcNow
        };
        _db.RefreshTokens.Add(newRt);

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("🔄 Refresh token rotado para usuario: {UserId}", storedToken.UserId);

        return new RefreshResponseDto(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(_accessTokenMinutes)
        );
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (storedToken is null) return false;

        storedToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("🚪 Refresh token revocado (logout): {UserId}", storedToken.UserId);
        return true;
    }

    public async Task<UserInfoDto?> GetUserInfoAsync(int userId, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive, ct);

        return user is null ? null : new UserInfoDto(
            user.Id, user.UserName, user.Rut, user.FullName, user.Role, user.IsActive,
            user.CreatedAt, user.LastLoginAt
        );
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || !VerifyPassword(dto.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;

        // Revocar todos los refresh tokens del usuario (forzar re-login)
        var userTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in userTokens) t.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("🔐 Password cambiado y tokens revocados para usuario: {UserId}", userId);
        return true;
    }

    public string HashPassword(string password)
        => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string hash)
        => BCrypt.Net.BCrypt.Verify(password, hash);

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("fullName", user.FullName),
            new("rut", user.Rut ?? string.Empty)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}