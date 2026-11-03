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
/// Endpoints de administración de usuarios (/api/users, solo Admin).
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization("RequireAdmin").RequireRateLimiting("api");

        // GET all users (paginado)
        users.MapGet("/", async (
            InventoryDbContext db,
            IValidator<UserFilterDto> filterValidator,
            [AsParameters] UserFilterDto filter) =>
        {
            var validation = await filterValidator.ValidateAsync(filter);
            if (!validation.IsValid) return ValidationProblem(validation);

            var query = db.Users.AsQueryable();

            if (filter.Role.HasValue)
                query = query.Where(u => u.Role == filter.Role.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(u => u.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(u => u.UserName.Contains(filter.Search!) || u.FullName.Contains(filter.Search!) || (u.Rut != null && u.Rut.Contains(filter.Search!)));

            query = query.OrderBy(u => u.UserName);

            var total = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ProjectToType<UserListDto>()
                .ToListAsync();

            var paged = PagedResult<UserListDto>.Create(items, total, filter.Page, filter.PageSize);
            return Results.Ok(OkResponse(paged, meta: new
            {
                pagination = new
                {
                    paged.TotalCount,
                    paged.Page,
                    paged.PageSize,
                    paged.TotalPages,
                    paged.HasPreviousPage,
                    paged.HasNextPage
                }
            }));
        })
        .WithName("GetUsers")
        .WithOpenApi();

        // GET user by ID
        users.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
        {
            var user = await db.Users
                .Where(u => u.Id == id)
                .ProjectToType<UserInfoDto>()
                .FirstOrDefaultAsync();
            return user is not null
                ? Results.Ok(OkResponse(user))
                : throw new NotFoundException("User", id);
        })
        .WithName("GetUserById")
        .WithOpenApi();

        // POST create user
        users.MapPost("/", async (CreateUserDto dto, IValidator<CreateUserDto> validator, InventoryDbContext db, IAuthService authService) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var user = dto.Adapt<User>();
            user.PasswordHash = authService.HashPassword(dto.Password);
            user.CreatedAt = DateTime.UtcNow;

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var result = user.Adapt<UserInfoDto>();
            return Results.Created($"/api/users/{user.Id}", OkResponse(result, "Usuario creado"));
        })
        .WithName("CreateUser")
        .WithOpenApi();

        // PUT update user
        users.MapPut("/{id:int}", async (int id, UpdateUserDto dto, IValidator<UpdateUserDto> validator, InventoryDbContext db) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var user = await db.Users.FindAsync(id);
            if (user is null) throw new NotFoundException("User", id);

            dto.Adapt(user);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var result = user.Adapt<UserInfoDto>();
            return Results.Ok(OkResponse(result, "Usuario actualizado"));
        })
        .WithName("UpdateUser")
        .WithOpenApi();

        // DELETE (soft delete) user
        users.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
        {
            var user = await db.Users.FindAsync(id);
            if (user is null) throw new NotFoundException("User", id);

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteUser")
        .WithOpenApi();
    }
}
