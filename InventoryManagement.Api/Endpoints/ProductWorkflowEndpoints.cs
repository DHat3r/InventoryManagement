using FluentValidation;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Common.Exceptions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Api.Services;
using static InventoryManagement.Api.Endpoints.EndpointHelpers;
using static InventoryManagement.Api.Endpoints.ProductEndpoints;

namespace InventoryManagement.Api.Endpoints;

/// <summary>
/// Flujo de aprobación y vigencia de productos (/api/products/{id}/...):
/// Admin aprueba (con precio) o rechaza; Admin retira directo e Inventario solicita el retiro; Admin reactiva.
/// </summary>
public static class ProductWorkflowEndpoints
{
    public static void MapProductWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var products = app.MapGroup("/api/products").WithTags("Products").RequireRateLimiting("api");

        // POST /approve: Admin aprueba un producto pendiente y define su precio de venta
        products.MapPost("/{id:int}/approve", async (
            int id, ApproveProductDto dto, IValidator<ApproveProductDto> validator,
            InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var product = await db.Products.FindAsync(id) ?? throw new NotFoundException("Product", id);
            if (product.ApprovalStatus != ProductApprovalStatus.Pendiente)
                throw new BusinessRuleException("NO_PENDIENTE", "Solo se pueden aprobar productos pendientes");

            product.Price = dto.Price;
            product.ApprovalStatus = ProductApprovalStatus.Aprobado;
            product.ApprovedBy = currentUser.UserId;
            product.ApprovedAt = DateTime.UtcNow;
            product.RejectionReason = null;
            product.ModifiedBy = currentUser.UserId;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(OkResponse(await LoadProductDtoAsync(db, id, currentUser), "Producto aprobado"));
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("ApproveProduct")
        .WithOpenApi();

        // POST /reject: Admin rechaza un producto pendiente (Inventario puede corregirlo y se reenvía)
        products.MapPost("/{id:int}/reject", async (
            int id, ReasonDto dto, IValidator<ReasonDto> validator,
            InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var product = await db.Products.FindAsync(id) ?? throw new NotFoundException("Product", id);
            if (product.ApprovalStatus != ProductApprovalStatus.Pendiente)
                throw new BusinessRuleException("NO_PENDIENTE", "Solo se pueden rechazar productos pendientes");

            product.ApprovalStatus = ProductApprovalStatus.Rechazado;
            product.RejectionReason = dto.Reason;
            product.ModifiedBy = currentUser.UserId;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(OkResponse(await LoadProductDtoAsync(db, id, currentUser), "Producto rechazado"));
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("RejectProduct")
        .WithOpenApi();

        // POST /withdraw: Admin retira directo. Inventario crea una solicitud de retiro;
        // mientras espera, el producto queda "posiblemente no disponible" (sin salidas).
        products.MapPost("/{id:int}/withdraw", async (
            int id, ReasonDto dto, IValidator<ReasonDto> validator,
            InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid) return ValidationProblem(validation);

            var product = await db.Products.FindAsync(id) ?? throw new NotFoundException("Product", id);
            if (product.Lifecycle != ProductLifecycle.Activo)
                throw new BusinessRuleException("NO_ACTIVO", "Solo se pueden retirar productos activos");

            var pendingWithdrawal = await db.ProductChangeRequests.FirstOrDefaultAsync(r =>
                r.ProductId == id && r.Type == ChangeRequestType.Retiro && r.Status == ChangeRequestStatus.Pendiente);

            if (currentUser.IsInRole(UserRole.Admin))
            {
                product.Lifecycle = ProductLifecycle.Retirado;
                product.ModifiedBy = currentUser.UserId;
                product.UpdatedAt = DateTime.UtcNow;

                // Si Inventario ya lo había pedido, la solicitud queda resuelta por esta acción
                if (pendingWithdrawal is not null)
                {
                    pendingWithdrawal.Status = ChangeRequestStatus.Aprobada;
                    pendingWithdrawal.ReviewedBy = currentUser.UserId;
                    pendingWithdrawal.ReviewedAt = DateTime.UtcNow;
                    pendingWithdrawal.ReviewComment = $"Retirado directamente por Admin: {dto.Reason}";
                }
                await db.SaveChangesAsync();

                return Results.Ok(OkResponse(await LoadProductDtoAsync(db, id, currentUser), "Producto retirado de la venta"));
            }

            if (pendingWithdrawal is not null)
                throw new BusinessRuleException("RETIRO_PENDIENTE", "Este producto ya tiene un retiro esperando aprobación de Admin");

            var request = new ProductChangeRequest
            {
                ProductId = id,
                Type = ChangeRequestType.Retiro,
                Reason = dto.Reason,
                RequestedBy = currentUser.UserId!.Value
            };
            db.ProductChangeRequests.Add(request);
            await db.SaveChangesAsync();

            return Results.Accepted($"/api/product-change-requests/{request.Id}",
                OkResponse(await LoadChangeRequestDtoAsync(db, request.Id),
                    "Retiro solicitado: el producto queda sin salidas hasta que Admin decida"));
        })
        .RequireAuthorization("RequireInventarioOrAdmin")
        .WithName("WithdrawProduct")
        .WithOpenApi();

        // POST /reactivate: Admin vuelve a poner a la venta un producto retirado
        products.MapPost("/{id:int}/reactivate", async (int id, InventoryDbContext db, ICurrentUserService currentUser) =>
        {
            var product = await db.Products.FindAsync(id) ?? throw new NotFoundException("Product", id);
            if (product.Lifecycle != ProductLifecycle.Retirado)
                throw new BusinessRuleException("NO_RETIRADO", "Solo se pueden reactivar productos retirados");

            product.Lifecycle = ProductLifecycle.Activo;
            product.ModifiedBy = currentUser.UserId;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(OkResponse(await LoadProductDtoAsync(db, id, currentUser), "Producto de nuevo a la venta"));
        })
        .RequireAuthorization("RequireAdmin")
        .WithName("ReactivateProduct")
        .WithOpenApi();
    }
}
