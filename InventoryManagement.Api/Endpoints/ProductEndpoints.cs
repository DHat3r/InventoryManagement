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
            ICurrentUserService currentUser,
            [AsParameters] ProductFilterDto filter) =>
        {
            var validation = await filterValidator.ValidateAsync(filter);
            if (!validation.IsValid) return ValidationProblem(validation);

            if (!CanSeeSalePrice(currentUser) && UsesSalePrice(filter))
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Acción no permitida",
                    detail: "Tu rol no puede filtrar ni ordenar por precio de venta");

            var query = db.Products.Include(p => p.Category).AsQueryable();

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (filter.Lifecycle.HasValue)
                query = query.Where(p => p.Lifecycle == filter.Lifecycle.Value);

            if (filter.ApprovalStatus.HasValue)
                query = query.Where(p => p.ApprovalStatus == filter.ApprovalStatus.Value);

            if (filter.OutOfStockOnly == true)
                query = query.Where(p => p.StockQuantity == 0);

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(p => p.Name.Contains(filter.Search!) || (p.SKU != null && p.SKU.Contains(filter.Search!)));

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.LowStockOnly == true)
                query = query.Where(p => p.Lifecycle == ProductLifecycle.Activo && p.StockQuantity <= p.MinStockLevel);

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
            items = items.Select(p => HideSalePriceIfNeeded(p, currentUser)).ToList();

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
        products.MapGet("/{id:int}", async (int id, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            return Results.Ok(OkResponse(await LoadProductDtoAsync(db, id, currentUser)));
        })
        .RequireAuthorization("RequireInventarioOrContadorOrAdmin")
        .WithName("GetProductById")
        .WithOpenApi();

        // GET low stock
        products.MapGet("/low-stock", async (InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var list = await db.Products
                .Include(p => p.Category)
                .Where(p => p.Lifecycle == ProductLifecycle.Activo && p.StockQuantity <= p.MinStockLevel)
                .OrderBy(p => p.StockQuantity)
                .ProjectToType<ProductListDto>()
                .ToListAsync();
            return Results.Ok(OkResponse(list.Select(p => HideSalePriceIfNeeded(p, currentUser)).ToList()));
        })
        .RequireAuthorization("RequireInventarioOrContadorOrAdmin") // Stock bajo: Inventario, Contador, Admin
        .WithName("GetLowStockProducts")
        .WithOpenApi();

        // POST create
        products.MapPost("/", async (CreateProductDto dto, IValidator<CreateProductDto> validator, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            // Admin crea con precio y queda aprobado. Inventario crea sin precio y queda
            // pendiente: el precio lo define Admin al aprobar (Inventario no ve precios de venta).
            var isAdmin = currentUser.IsInRole(UserRole.Admin);
            if (isAdmin && !dto.Price.HasValue)
                return PriceProblem("Al crear un producto como Admin hay que indicar el precio de venta");
            if (!isAdmin && dto.Price.HasValue)
                return PriceProblem("El precio de venta lo define Admin al aprobar el producto");

            var entity = dto.Adapt<Product>();
            entity.CreatedBy = currentUser.UserId;
            entity.ApprovalStatus = isAdmin ? ProductApprovalStatus.Aprobado : ProductApprovalStatus.Pendiente;
            if (isAdmin)
            {
                entity.ApprovedBy = currentUser.UserId;
                entity.ApprovedAt = DateTime.UtcNow;
            }
            db.Products.Add(entity);
            await db.SaveChangesAsync();

            var result = await LoadProductDtoAsync(db, entity.Id, currentUser);
            var message = isAdmin ? "Producto creado" : "Producto creado, pendiente de aprobación de Admin";
            return Results.Created($"/api/products/{entity.Id}", OkResponse(result, message));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Crear productos: Inventario (queda pendiente), Admin
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

            var isAdmin = currentUser.IsInRole(UserRole.Admin);
            if (!isAdmin && dto.Price.HasValue)
                return PriceProblem("El precio de venta solo lo cambia Admin");

            // Inventario sobre un producto ya aprobado: no se aplica, queda como cambio propuesto
            if (!isAdmin && entity.ApprovalStatus == ProductApprovalStatus.Aprobado)
            {
                var hasPendingEdit = await db.ProductChangeRequests.AnyAsync(r =>
                    r.ProductId == id && r.Type == ChangeRequestType.Edicion && r.Status == ChangeRequestStatus.Pendiente);
                if (hasPendingEdit)
                    throw new BusinessRuleException("EDICION_PENDIENTE",
                        "Este producto ya tiene una edición esperando aprobación de Admin");

                var request = new ProductChangeRequest
                {
                    ProductId = id,
                    Type = ChangeRequestType.Edicion,
                    Name = dto.Name,
                    SKU = dto.SKU,
                    Description = dto.Description,
                    Cost = dto.Cost,
                    CategoryId = dto.CategoryId,
                    MinStockLevel = dto.MinStockLevel,
                    UnitOfMeasure = dto.UnitOfMeasure,
                    RequestedBy = currentUser.UserId!.Value
                };
                db.ProductChangeRequests.Add(request);
                await db.SaveChangesAsync();

                var requestDto = await LoadChangeRequestDtoAsync(db, request.Id);
                return Results.Accepted($"/api/product-change-requests/{request.Id}",
                    OkResponse(requestDto, "Cambio enviado a Admin para aprobación"));
            }

            // Admin, o Inventario sobre su borrador (pendiente o rechazado): se aplica directo
            dto.Adapt(entity);
            if (isAdmin && dto.Price.HasValue) entity.Price = dto.Price;
            if (entity.ApprovalStatus == ProductApprovalStatus.Rechazado)
            {
                // Editar un producto rechazado lo vuelve a enviar a revisión
                entity.ApprovalStatus = ProductApprovalStatus.Pendiente;
                entity.RejectionReason = null;
            }
            entity.ModifiedBy = currentUser.UserId;
            await db.SaveChangesAsync();

            var result = await LoadProductDtoAsync(db, id, currentUser);
            return Results.Ok(OkResponse(result, "Producto actualizado"));
        })
        .RequireAuthorization("RequireInventarioOrAdmin") // Actualizar: Admin directo; Inventario propone si ya está aprobado
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

            var result = await LoadProductDtoAsync(db, id, currentUser);
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

            // Borrar = descontinuar (soft delete): el historial del kardex se conserva
            entity.Lifecycle = ProductLifecycle.Descontinuado;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ModifiedBy = currentUser.UserId;
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .RequireAuthorization("RequireAdmin") // Eliminar productos: solo Admin
        .WithName("DeleteProduct")
        .WithOpenApi();
    }

    // Carga el producto como DTO desde la base (con categoría y solicitudes pendientes) y oculta el precio si corresponde.
    // Se usa también en las respuestas de escritura para que siempre muestren el estado real.
    internal static async Task<ProductDto> LoadProductDtoAsync(InventoryDbContext db, int id, ICurrentUserService currentUser)
    {
        var dto = await db.Products
            .Where(p => p.Id == id)
            .ProjectToType<ProductDto>()
            .FirstOrDefaultAsync();
        return dto is null
            ? throw new NotFoundException("Product", id)
            : HideSalePriceIfNeeded(dto, currentUser);
    }

    internal static async Task<ProductChangeRequestDto> LoadChangeRequestDtoAsync(InventoryDbContext db, int id)
    {
        var dto = await db.ProductChangeRequests
            .Where(r => r.Id == id)
            .ProjectToType<ProductChangeRequestDto>()
            .FirstOrDefaultAsync();
        return dto ?? throw new NotFoundException("ProductChangeRequest", id);
    }

    private static IResult PriceProblem(string detail)
        => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Precio no permitido", detail: detail);

    // El precio de venta (y el margen) es información comercial: Inventario no lo ve,
    // para evitar fugas de datos y que se confunda con el costo. Admin y Contador sí.
    private static bool CanSeeSalePrice(ICurrentUserService currentUser)
        => currentUser.IsInRole(UserRole.Admin, UserRole.Contador);

    private static ProductDto HideSalePriceIfNeeded(ProductDto dto, ICurrentUserService currentUser)
        => CanSeeSalePrice(currentUser) ? dto : dto with { Price = null, MarginPercentage = null };

    private static ProductListDto HideSalePriceIfNeeded(ProductListDto dto, ICurrentUserService currentUser)
        => CanSeeSalePrice(currentUser) ? dto : dto with { Price = null };

    // Filtrar u ordenar por precio permitiría deducirlo aunque la respuesta no lo traiga
    private static bool UsesSalePrice(ProductFilterDto filter)
        => filter.MinPrice.HasValue
           || filter.MaxPrice.HasValue
           || (filter.SortBy?.Contains("price", StringComparison.OrdinalIgnoreCase) ?? false);

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
