using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Common.Exceptions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Api.Services;
using static InventoryManagement.Api.Endpoints.EndpointHelpers;
using static InventoryManagement.Api.Endpoints.ProductEndpoints;

namespace InventoryManagement.Api.Endpoints;

/// <summary>
/// Bandeja de Admin para las solicitudes de Inventario (/api/product-change-requests):
/// ediciones de productos aprobados y retiros de la venta.
/// </summary>
public static class ChangeRequestEndpoints
{
    public static void MapChangeRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var requests = app.MapGroup("/api/product-change-requests")
            .WithTags("Product change requests")
            .RequireAuthorization("RequireAdmin")
            .RequireRateLimiting("api");

        // GET: por defecto solo las pendientes, de la más antigua a la más nueva
        requests.MapGet("/", async (InventoryDbContext db, ChangeRequestStatus? status, ChangeRequestType? type) =>
        {
            var query = db.ProductChangeRequests.Where(r => r.Status == (status ?? ChangeRequestStatus.Pendiente));
            if (type.HasValue) query = query.Where(r => r.Type == type.Value);

            var list = await query
                .OrderBy(r => r.RequestedAt)
                .ProjectToType<ProductChangeRequestDto>()
                .ToListAsync();
            return Results.Ok(OkResponse(list));
        })
        .WithName("GetProductChangeRequests")
        .WithOpenApi();

        // POST /approve: Edicion copia los valores propuestos al producto; Retiro lo retira de la venta
        requests.MapPost("/{id:int}/approve", async (
            int id, ReviewChangeRequestDto dto, IValidator<ReviewChangeRequestDto> validator,
            InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var request = await db.ProductChangeRequests.Include(r => r.Product).FirstOrDefaultAsync(r => r.Id == id)
                          ?? throw new NotFoundException("ProductChangeRequest", id);
            if (request.Status != ChangeRequestStatus.Pendiente)
                throw new BusinessRuleException("SOLICITUD_RESUELTA", "La solicitud ya fue revisada");

            var product = request.Product;
            if (request.Type == ChangeRequestType.Edicion)
            {
                // El SKU pudo haberse ocupado entre que se propuso y se aprueba
                if (!string.IsNullOrWhiteSpace(request.SKU)
                    && await db.Products.AnyAsync(p => p.Id != product.Id && p.SKU == request.SKU))
                    throw new BusinessRuleException("SKU_DUPLICADO", $"Otro producto ya usa el SKU {request.SKU}");

                product.Name = request.Name!;
                product.SKU = request.SKU;
                product.Description = request.Description;
                product.Cost = request.Cost!.Value;
                product.CategoryId = request.CategoryId!.Value;
                product.MinStockLevel = request.MinStockLevel!.Value;
                product.UnitOfMeasure = request.UnitOfMeasure;
            }
            else
            {
                if (product.Lifecycle == ProductLifecycle.Activo)
                    product.Lifecycle = ProductLifecycle.Retirado;
            }

            product.ModifiedBy = currentUser.UserId;
            product.UpdatedAt = DateTime.UtcNow;
            MarkReviewed(request, ChangeRequestStatus.Aprobada, dto.Comment, currentUser);
            await db.SaveChangesAsync();

            return Results.Ok(OkResponse(await LoadChangeRequestDtoAsync(db, id), "Solicitud aprobada"));
        })
        .WithName("ApproveProductChangeRequest")
        .WithOpenApi();

        // POST /reject: no cambia el producto. Un retiro rechazado deja de bloquear las salidas.
        requests.MapPost("/{id:int}/reject", async (
            int id, ReviewChangeRequestDto dto, IValidator<ReviewChangeRequestDto> validator,
            InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var request = await db.ProductChangeRequests.FindAsync(id)
                          ?? throw new NotFoundException("ProductChangeRequest", id);
            if (request.Status != ChangeRequestStatus.Pendiente)
                throw new BusinessRuleException("SOLICITUD_RESUELTA", "La solicitud ya fue revisada");

            MarkReviewed(request, ChangeRequestStatus.Rechazada, dto.Comment, currentUser);
            await db.SaveChangesAsync();

            return Results.Ok(OkResponse(await LoadChangeRequestDtoAsync(db, id), "Solicitud rechazada"));
        })
        .WithName("RejectProductChangeRequest")
        .WithOpenApi();
    }

    private static void MarkReviewed(ProductChangeRequest request, ChangeRequestStatus status, string? comment, ICurrentUserService currentUser)
    {
        request.Status = status;
        request.ReviewedBy = currentUser.UserId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewComment = comment;
    }
}
