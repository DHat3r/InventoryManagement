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
/// Endpoints de movimientos de stock y kardex (/api/products/{id}/movements y /kardex).
/// </summary>
public static class KardexEndpoints
{
    public static void MapKardexEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products").RequireRateLimiting("api");

        // POST /api/products/{id}/movements - Registrar movimiento de stock
        products.MapPost("/{id:int}/movements", async (
            int id,
            CreateStockMovementDto dto,
            IValidator<CreateStockMovementDto> validator,
            InventoryDbContext db,
            ICurrentUserService currentUser) =>
        {
            // Autorización según tipo de movimiento
            if (dto.Type == MovementType.Entrada)
            {
                if (!currentUser.IsInRole(UserRole.Comprador, UserRole.Inventario, UserRole.Admin))
                    return Results.Forbid();
            }
            else if (dto.Type == MovementType.Salida)
            {
                if (!currentUser.IsInRole(UserRole.Inventario, UserRole.Admin))
                    return Results.Forbid();
            }
            else if (dto.Type == MovementType.Ajuste)
            {
                if (!currentUser.IsInRole(UserRole.Inventario, UserRole.Admin))
                    return Results.Forbid();
            }

            // Pasar productId al validador ANTES de validar (para validaciones de negocio con BD)
            if (validator is CreateStockMovementValidator csv) csv.SetProductId(id);

            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var product = await db.Products.FindAsync(id);
            if (product is null) throw new NotFoundException("Product", id);

            // Calcular stock anterior y nuevo según tipo de movimiento
            int previousStock = product.StockQuantity;
            int newStock;
            decimal? unitCost = dto.UnitCost;

            switch (dto.Type)
            {
                case MovementType.Entrada:
                    newStock = previousStock + dto.Quantity;
                    // UnitCost requerido para valoración de entrada
                    if (!unitCost.HasValue) unitCost = product.Cost;
                    break;

                case MovementType.Salida:
                    if (previousStock < dto.Quantity)
                        throw new BusinessRuleException("STOCK_INSUFICIENTE",
                            $"Stock insuficiente. Disponible: {previousStock}, Solicitado: {dto.Quantity}");
                    newStock = previousStock - dto.Quantity;
                    // Para salidas usamos costo promedio actual si no se provee
                    unitCost ??= product.Cost;
                    break;

                case MovementType.Ajuste:
                    // Ajuste: Quantity positivo = entrada, negativo = salida
                    // Usamos UnitCost como indicador: si tiene valor = entrada, si no = salida
                    if (unitCost.HasValue)
                    {
                        newStock = previousStock + dto.Quantity;
                    }
                    else
                    {
                        if (previousStock < dto.Quantity)
                            throw new BusinessRuleException("STOCK_INSUFICIENTE",
                                $"Stock insuficiente para ajuste negativo. Disponible: {previousStock}");
                        newStock = previousStock - dto.Quantity;
                    }
                    unitCost ??= product.Cost;
                    break;

                default:
                    throw new BusinessRuleException("TIPO_MOVIMIENTO_INVALIDO", "Tipo de movimiento no reconocido");
            }

            // Crear movimiento en transacción
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var movement = dto.Adapt<StockMovement>();
                movement.ProductId = id;
                movement.PreviousStock = previousStock;
                movement.NewStock = newStock;
                movement.UnitCost = unitCost;
                movement.UserId = currentUser.UserId; // Auditoría: quién registró el movimiento

                db.StockMovements.Add(movement);

                // Actualizar stock del producto
                product.StockQuantity = newStock;
                product.UpdatedAt = DateTime.UtcNow;
                product.ModifiedBy = currentUser.UserId;

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = movement.Adapt<StockMovementDto>();
                return Results.Created($"/api/products/{id}/movements/{movement.Id}", OkResponse(result, "Movimiento registrado"));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        })
        // .RequireAuthorization("RequireCompradorOrInventarioOrAdmin") // Manejado dentro del endpoint según tipo
        .WithName("CreateStockMovement")
        .WithOpenApi();

        // GET /api/products/{id}/kardex - Historial paginado de movimientos
        products.MapGet("/{id:int}/kardex", async (
            int id,
            InventoryDbContext db,
            IValidator<StockMovementFilterDto> filterValidator,
            ICurrentUserService currentUser,
            [AsParameters] StockMovementFilterDto filter) =>
        {
            var validation = await filterValidator.ValidateAsync(filter);
            if (!validation.IsValid) return ValidationProblem(validation);

            var product = await db.Products.FindAsync(id);
            if (product is null) throw new NotFoundException("Product", id);

            var query = db.StockMovements
                .Where(m => m.ProductId == id)
                .OrderByDescending(m => m.CreatedAt)
                .AsQueryable();

            if (filter.Type.HasValue)
                query = query.Where(m => m.Type == filter.Type.Value);

            if (filter.DateFrom.HasValue)
                query = query.Where(m => m.CreatedAt >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(m => m.CreatedAt <= filter.DateTo.Value);

            if (!string.IsNullOrWhiteSpace(filter.Reference))
                query = query.Where(m => m.Reference != null && m.Reference.Contains(filter.Reference!));

            var total = await query.CountAsync();
            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ProjectToType<StockMovementListDto>()
                .ToListAsync();

            // Los valores son información contable: Inventario ve las cantidades, pero no el costo unitario
            if (!currentUser.IsInRole(UserRole.Admin, UserRole.Contador))
                items = items.Select(m => m with { UnitCost = null }).ToList();

            var paged = PagedResult<StockMovementListDto>.Create(items, total, filter.Page, filter.PageSize);
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
        .RequireAuthorization("RequireInventarioOrContadorOrAdmin") // Kardex de stock: Inventario, Contador, Admin (sin costos para Inventario)
        .WithName("GetProductKardex")
        .WithOpenApi();

        // GET /api/products/{id}/kardex/summary - Resumen valorado del kardex
        products.MapGet("/{id:int}/kardex/summary", async (int id, InventoryDbContext db) =>
        {
            var product = await db.Products.FindAsync(id);
            if (product is null) throw new NotFoundException("Product", id);

            var movements = await db.StockMovements
                .Where(m => m.ProductId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            int totalEntradas = movements.Where(m => m.Type == MovementType.Entrada).Sum(m => m.Quantity);
            int totalSalidas = movements.Where(m => m.Type == MovementType.Salida).Sum(m => m.Quantity);
            int totalAjustes = movements.Where(m => m.Type == MovementType.Ajuste).Sum(m => m.Quantity);

            decimal totalEntradasValue = movements
                .Where(m => m.Type == MovementType.Entrada)
                .Sum(m => m.Quantity * (m.UnitCost ?? 0));

            decimal totalSalidasValue = movements
                .Where(m => m.Type == MovementType.Salida)
                .Sum(m => m.Quantity * (m.UnitCost ?? 0));

            // Costo promedio ponderado de las entradas
            decimal averageCost = totalEntradas > 0
                ? Math.Round(totalEntradasValue / totalEntradas, 2)
                : product.Cost;

            var summary = new KardexSummaryDto(
                ProductId: product.Id,
                ProductName: product.Name,
                ProductSKU: product.SKU ?? string.Empty,
                CurrentStock: product.StockQuantity,
                AverageCost: averageCost,
                StockValue: Math.Round(product.StockQuantity * averageCost, 2),
                TotalEntradas: totalEntradas,
                TotalSalidas: totalSalidas,
                TotalAjustes: totalAjustes,
                TotalEntradasValue: Math.Round(totalEntradasValue, 2),
                TotalSalidasValue: Math.Round(totalSalidasValue, 2)
            );

            return Results.Ok(OkResponse(summary));
        })
        .RequireAuthorization("RequireContadorOrAdmin") // Resumen valorado: Contador, Admin
        .WithName("GetProductKardexFiltered")
        .WithOpenApi();
    }
}
