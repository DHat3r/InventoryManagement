using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.Common.Exceptions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Api.Services;
using InventoryManagement.Api.Validators;
using static InventoryManagement.Api.Endpoints.EndpointHelpers;

namespace InventoryManagement.Api.Endpoints;

/// <summary>
/// Endpoints de autenticación (/api/auth): login, refresh, logout, me y cambio de contraseña.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous().RequireRateLimiting("auth");

        // POST /api/auth/login
        auth.MapPost("/login", async (LoginDto dto, IValidator<LoginDto> validator, IAuthService authService) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var result = await authService.LoginAsync(dto);
            return result is not null
                ? Results.Ok(OkResponse(result, "Login exitoso"))
                : Results.Unauthorized();
        })
        .WithName("Login")
        .WithOpenApi();

        // POST /api/auth/refresh
        auth.MapPost("/refresh", async (RefreshTokenDto dto, IValidator<RefreshTokenDto> validator, IAuthService authService) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var result = await authService.RefreshAsync(dto.RefreshToken);
            return result is not null
                ? Results.Ok(OkResponse(result, "Token renovado"))
                : Results.Unauthorized();
        })
        .WithName("RefreshToken")
        .WithOpenApi();

        // POST /api/auth/logout
        auth.MapPost("/logout", async (RefreshTokenDto dto, IValidator<RefreshTokenDto> validator, IAuthService authService) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            await authService.RevokeRefreshTokenAsync(dto.RefreshToken);
            return Results.Ok(OkResponse(true, "Logout exitoso"));
        })
        .RequireAuthorization() // Requiere estar autenticado
        .WithName("Logout")
        .WithOpenApi();

        // GET /api/auth/me
        auth.MapGet("/me", async (ICurrentUserService currentUser, IAuthService authService) =>
        {
            if (!currentUser.IsAuthenticated || !currentUser.UserId.HasValue)
                return Results.Unauthorized();

            var userInfo = await authService.GetUserInfoAsync(currentUser.UserId.Value);
            return userInfo is not null
                ? Results.Ok(OkResponse(userInfo))
                : Results.NotFound();
        })
        .RequireAuthorization()
        .WithName("GetCurrentUser")
        .WithOpenApi();

        // POST /api/auth/change-password
        auth.MapPost("/change-password", async (ChangePasswordDto dto, IValidator<ChangePasswordDto> validator, IAuthService authService, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            if (!currentUser.UserId.HasValue) return Results.Unauthorized();

            var success = await authService.ChangePasswordAsync(currentUser.UserId.Value, dto);
            return success
                ? Results.Ok(OkResponse(true, "Contraseña cambiada exitosamente"))
                : Results.BadRequest(OkResponse<bool>(false, "Contraseña actual incorrecta"));
        })
        .RequireAuthorization()
        .WithName("ChangePassword")
        .WithOpenApi();
    }
}
