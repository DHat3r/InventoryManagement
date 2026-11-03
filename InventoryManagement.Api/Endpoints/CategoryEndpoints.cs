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
/// Endpoints de categorías (/api/categories): CRUD y árbol jerárquico.
/// </summary>
public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var categories = app.MapGroup("/api/categories").WithTags("Categories").RequireRateLimiting("api");

        // GET all
        categories.MapGet("/", async (InventoryDbContext db) =>
        {
            var list = await db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ProjectToType<CategoryDto>()
                .ToListAsync();
            return Results.Ok(OkResponse(list));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Ver categorías: Inventario, Admin
        .WithName("GetCategories")
        .WithOpenApi();

        // GET by ID
        categories.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
        {
            var entity = await db.Categories
                .Where(c => c.Id == id)
                .ProjectToType<CategoryDto>()
                .FirstOrDefaultAsync();
            return entity is not null 
                ? Results.Ok(OkResponse(entity)) 
                : throw new NotFoundException("Category", id);
        })
        .RequireAuthorization("RequireInventarioOrAdmin")
        .WithName("GetCategoryById")
        .WithOpenApi();

        // POST create
        categories.MapPost("/", async (CreateCategoryDto dto, IValidator<CreateCategoryDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var entity = dto.Adapt<Category>();
            entity.CreatedBy = currentUser.UserId;
            db.Categories.Add(entity);
            await db.SaveChangesAsync();

            var result = entity.Adapt<CategoryDto>();
            return Results.Created($"/api/categories/{entity.Id}", OkResponse(result, "Categoría creada"));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Crear categorías: Inventario, Admin
        .WithName("CreateCategory")
        .WithOpenApi();

        // PUT update
        categories.MapPut("/{id:int}", async (int id, UpdateCategoryDto dto, IValidator<UpdateCategoryDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            // Pasar el ID al validador ANTES de validar (unicidad y referencias circulares)
            if (validator is UpdateCategoryValidator ucv) ucv.SetCategoryId(id);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var entity = await db.Categories.FindAsync(id);
            if (entity is null) throw new NotFoundException("Category", id);

            dto.Adapt(entity);
            entity.ModifiedBy = currentUser.UserId;
            await db.SaveChangesAsync();

            var result = entity.Adapt<CategoryDto>();
            return Results.Ok(OkResponse(result, "Categoría actualizada"));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Actualizar categorías: Inventario, Admin
        .WithName("UpdateCategory")
        .WithOpenApi();

        // DELETE (soft)
        categories.MapDelete("/{id:int}", async (int id, InventoryDbContext db) =>
        {
            var entity = await db.Categories.FindAsync(id);
            if (entity is null) throw new NotFoundException("Category", id);

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization("RequireAdmin") // Eliminar categorías: solo Admin
        .WithName("DeleteCategory")
        .WithOpenApi();


        // GET tree (jerarquía de categorías)
        categories.MapGet("/tree", async (InventoryDbContext db) =>
        {
            // Cargar todas las categorías activas
            var allCategories = await db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ProjectToType<CategoryTreeDto>()
                .ToListAsync();

            // Construir árbol manualmente (evita recursión de Mapster)
            var lookup = allCategories.ToDictionary(c => c.Id);
            var roots = new List<CategoryTreeDto>();

            foreach (var cat in allCategories)
            {
                if (cat.ParentCategoryId.HasValue && lookup.TryGetValue(cat.ParentCategoryId.Value, out var parent))
                {
                    parent.Children.Add(cat);
                }
                else
                {
                    roots.Add(cat);
                }
            }

            return Results.Ok(OkResponse(roots));
        })
        .RequireAuthorization("RequireInventarioOrAdmin")
        .WithName("GetCategoriesTree")
        .WithOpenApi();
    }
}
