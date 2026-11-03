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
/// Endpoints del catálogo de productos (/api/products): CRUD, filtros, stock bajo y actualización de stock.
/// </summary>
public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products").RequireRateLimiting("api");

        // GET all (paginado, filtros, orden)
        products.MapGet("/", async (
            InventoryDbContext db,
            IValidator<ProductFilterDto> filterValidator,
            [AsParameters] ProductFilterDto filter) =>
        {
            var validation = await filterValidator.ValidateAsync(filter);
            if (!validation.IsValid) return ValidationProblem(validation);

            var query = db.Products.Include(p => p.Category).AsQueryable();

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(p => p.Name.Contains(filter.Search!) || (p.SKU != null && p.SKU.Contains(filter.Search!)));

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.LowStockOnly == true)
                query = query.Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel);

            // Filtros por rango de fechas (convertir a UTC para PostgreSQL)
            if (filter.CreatedAtFrom.HasValue)
            {
                var utc = filter.CreatedAtFrom.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(filter.CreatedAtFrom.Value, DateTimeKind.Utc)
                    : filter.CreatedAtFrom.Value.ToUniversalTime();
                query = query.Where(p => p.CreatedAt >= utc);
            }

            if (filter.CreatedAtTo.HasValue)
            {
                var utc = filter.CreatedAtTo.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(filter.CreatedAtTo.Value, DateTimeKind.Utc)
                    : filter.CreatedAtTo.Value.ToUniversalTime();
                query = query.Where(p => p.CreatedAt <= utc);
            }

            if (filter.UpdatedAtFrom.HasValue)
            {
                var utc = filter.UpdatedAtFrom.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(filter.UpdatedAtFrom.Value, DateTimeKind.Utc)
                    : filter.UpdatedAtFrom.Value.ToUniversalTime();
                query = query.Where(p => p.UpdatedAt >= utc);
            }

            if (filter.UpdatedAtTo.HasValue)
            {
                var utc = filter.UpdatedAtTo.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(filter.UpdatedAtTo.Value, DateTimeKind.Utc)
                    : filter.UpdatedAtTo.Value.ToUniversalTime();
                query = query.Where(p => p.UpdatedAt <= utc);
            }

            // Ordenamiento dinámico multi-campo
            query = ApplyMultiSort(query, filter.SortBy, filter.SortDesc);

            var total = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ProjectToType<ProductListDto>()
                .ToListAsync();

            var paged = PagedResult<ProductListDto>.Create(items, total, filter.Page, filter.PageSize);
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
        .RequireAuthorization("RequireInventarioOrContadorOrAdmin") // Ver productos: Inventario, Contador (solo lectura), Admin
        .WithName("GetProducts")
        .WithOpenApi();

        // GET by ID (completo)
        products.MapGet("/{id:int}", async (int id, InventoryDbContext db) =>
        {
            var entity = await db.Products
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .ProjectToType<ProductDto>()
                .FirstOrDefaultAsync();
            return entity is not null 
                ? Results.Ok(OkResponse(entity)) 
                : throw new NotFoundException("Product", id);
        })
        .RequireAuthorization("RequireInventarioOrContadorOrAdmin")
        .WithName("GetProductById")
        .WithOpenApi();

        // GET low stock
        products.MapGet("/low-stock", async (InventoryDbContext db) =>
        {
            var list = await db.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity <= p.MinStockLevel)
                .OrderBy(p => p.StockQuantity)
                .ProjectToType<ProductListDto>()
                .ToListAsync();
            return Results.Ok(OkResponse(list));
        })
        .RequireAuthorization("RequireInventarioOrContadorOrAdmin") // Stock bajo: Inventario, Contador, Admin
        .WithName("GetLowStockProducts")
        .WithOpenApi();

        // POST create
        products.MapPost("/", async (CreateProductDto dto, IValidator<CreateProductDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var entity = dto.Adapt<Product>();
            entity.CreatedBy = currentUser.UserId;
            db.Products.Add(entity);
            await db.SaveChangesAsync();

            // Recargar con Category para el DTO de salida
            await db.Entry(entity).Reference(p => p.Category).LoadAsync();
            var result = entity.Adapt<ProductDto>();

            return Results.Created($"/api/products/{entity.Id}", OkResponse(result, "Producto creado"));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Crear productos: Inventario, Admin
        .WithName("CreateProduct")
        .WithOpenApi();

        // PUT update
        products.MapPut("/{id:int}", async (int id, UpdateProductDto dto, IValidator<UpdateProductDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            // Pasar ID al validador ANTES de validar para unicidad de SKU
            if (validator is UpdateProductValidator upv) upv.SetProductId(id);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var entity = await db.Products.FindAsync(id);
            if (entity is null) throw new NotFoundException("Product", id);

            dto.Adapt(entity);
            entity.ModifiedBy = currentUser.UserId;
            await db.SaveChangesAsync();

            await db.Entry(entity).Reference(p => p.Category).LoadAsync();
            var result = entity.Adapt<ProductDto>();

            return Results.Ok(OkResponse(result, "Producto actualizado"));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Actualizar productos: Inventario, Admin
        .WithName("UpdateProduct")
        .WithOpenApi();

        // PATCH stock
        products.MapPatch("/{id:int}/stock", async (int id, UpdateStockDto dto, IValidator<UpdateStockDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var entity = await db.Products.FindAsync(id);
            if (entity is null) throw new NotFoundException("Product", id);

            dto.Adapt(entity);
            entity.ModifiedBy = currentUser.UserId;
            await db.SaveChangesAsync();

            var result = entity.Adapt<ProductDto>();
            return Results.Ok(OkResponse(result, "Stock actualizado"));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Actualizar stock: Inventario, Admin
        .WithName("UpdateProductStock")
        .WithOpenApi();

        // DELETE (soft)
        products.MapDelete("/{id:int}", async (int id, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var entity = await db.Products.FindAsync(id);
            if (entity is null) throw new NotFoundException("Product", id);

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser.UserId;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization("RequireAdmin") // Eliminar productos: solo Admin
        .WithName("DeleteProduct")
        .WithOpenApi();
    }

    // Aplica ordenamiento multi-campo: "price:desc,name:asc,stock"
    private static IQueryable<Product> ApplyMultiSort(IQueryable<Product> query, string? sortBy, bool sortDesc)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
            return sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name);

        var sorts = sortBy.Split(',', StringSplitOptions.RemoveEmptyEntries);
        IOrderedQueryable<Product>? ordered = null;

        foreach (var sort in sorts)
        {
            var parts = sort.Split(':', StringSplitOptions.RemoveEmptyEntries);
            var field = parts[0].Trim().ToLower();
            var direction = parts.Length > 1 ? parts[1].Trim().ToLower() : (sortDesc ? "desc" : "asc");
            var descending = direction == "desc";

            var isFirst = ordered == null;
            ordered = (field, descending, isFirst) switch
            {
                ("price", true, true) => query.OrderByDescending(p => p.Price),
                ("price", false, true) => query.OrderBy(p => p.Price),
                ("stock", true, true) => query.OrderByDescending(p => p.StockQuantity),
                ("stock", false, true) => query.OrderBy(p => p.StockQuantity),
                ("createdat", true, true) => query.OrderByDescending(p => p.CreatedAt),
                ("createdat", false, true) => query.OrderBy(p => p.CreatedAt),
                ("updatedat", true, true) => query.OrderByDescending(p => p.UpdatedAt),
                ("updatedat", false, true) => query.OrderBy(p => p.UpdatedAt),
                ("name", true, true) => query.OrderByDescending(p => p.Name),
                ("name", false, true) => query.OrderBy(p => p.Name),
                ("price", true, false) => ordered!.ThenByDescending(p => p.Price),
                ("price", false, false) => ordered!.ThenBy(p => p.Price),
                ("stock", true, false) => ordered!.ThenByDescending(p => p.StockQuantity),
                ("stock", false, false) => ordered!.ThenBy(p => p.StockQuantity),
                ("createdat", true, false) => ordered!.ThenByDescending(p => p.CreatedAt),
                ("createdat", false, false) => ordered!.ThenBy(p => p.CreatedAt),
                ("updatedat", true, false) => ordered!.ThenByDescending(p => p.UpdatedAt),
                ("updatedat", false, false) => ordered!.ThenBy(p => p.UpdatedAt),
                ("name", true, false) => ordered!.ThenByDescending(p => p.Name),
                ("name", false, false) => ordered!.ThenBy(p => p.Name),
                _ => isFirst ? (descending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)) : ordered!
            };
        }

        return ordered ?? (sortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name));
    }
}
