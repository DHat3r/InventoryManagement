using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Api.Services;

/// <summary>
/// Servicio para acceder al usuario autenticado actual
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? UserName { get; }
    UserRole? Role { get; }
    string? Rut { get; }
    string? FullName { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(params UserRole[] roles);
    Task<User?> GetUserAsync(CancellationToken ct = default);
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly InventoryDbContext _db;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, InventoryDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _db = db;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int? UserId
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                ?? User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? UserName => User?.FindFirst(ClaimTypes.Name)?.Value 
        ?? User?.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

    public UserRole? Role
    {
        get
        {
            var claim = User?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(claim, out var role) ? role : null;
        }
    }

    public string? Rut => User?.FindFirst("rut")?.Value;

    public string? FullName => User?.FindFirst("fullName")?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(params UserRole[] roles)
    {
        var role = Role;
        return role.HasValue && roles.Contains(role.Value);
    }

    public async Task<User?> GetUserAsync(CancellationToken ct = default)
    {
        if (!UserId.HasValue) return null;
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId.Value && u.IsActive, ct);
    }
}