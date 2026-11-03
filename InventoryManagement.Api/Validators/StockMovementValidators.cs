using FluentValidation;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Validators;

/// <summary>
/// Validador para crear movimiento de stock
/// </summary>
public class CreateStockMovementValidator : AbstractValidator<CreateStockMovementDto>
{
    private readonly InventoryDbContext _db;
    private int _productId;

    public CreateStockMovementValidator(InventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Tipo de movimiento inválido");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");

        RuleFor(x => x.Reason)
            .MaximumLength(200).WithMessage("El motivo no puede exceder 200 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Reason));

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("La referencia no puede exceder 100 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Reference));

        RuleFor(x => x.UnitCost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo unitario no puede ser negativo")
            .When(x => x.UnitCost.HasValue);

        // Validaciones de negocio según tipo: cada regla informa su propio motivo
        RuleFor(x => x).CustomAsync(async (dto, context, ct) =>
        {
            var error = await FindBusinessRuleError(dto, ct);
            if (error is not null) context.AddFailure(error.Value.Property, error.Value.Message);
        });
    }

    public CreateStockMovementValidator SetProductId(int productId)
    {
        _productId = productId;
        return this;
    }

    // Devuelve null si el movimiento es válido, o el campo y el motivo del rechazo
    private async Task<(string Property, string Message)?> FindBusinessRuleError(CreateStockMovementDto dto, CancellationToken ct)
    {
        if (_productId <= 0) return ("", "Producto no válido");

        var product = await _db.Products
            .Where(p => p.Id == _productId && p.IsActive)
            .FirstOrDefaultAsync(ct);

        if (product is null) return ("", "El producto no existe o está inactivo");

        return dto.Type switch
        {
            MovementType.Entrada => ValidateEntrada(dto),
            MovementType.Salida => ValidateSalida(dto, product),
            MovementType.Ajuste => ValidateAjuste(dto, product),
            _ => (nameof(dto.Type), "Tipo de movimiento inválido")
        };
    }

    private static (string, string)? ValidateEntrada(CreateStockMovementDto dto)
    {
        // Entrada: UnitCost requerido (para valoración)
        return dto.UnitCost.HasValue
            ? null
            : (nameof(dto.UnitCost), "El costo unitario es obligatorio para una entrada");
    }

    private static (string, string)? ValidateSalida(CreateStockMovementDto dto, Product product)
    {
        // Salida: stock suficiente. UnitCost opcional (se usa el costo del producto)
        return product.StockQuantity < dto.Quantity
            ? (nameof(dto.Quantity), $"Stock insuficiente. Disponible: {product.StockQuantity}, solicitado: {dto.Quantity}")
            : null;
    }

    private static (string, string)? ValidateAjuste(CreateStockMovementDto dto, Product product)
    {
        // Ajuste: con UnitCost suma stock, sin UnitCost resta. El stock final no puede ser negativo.
        var newStock = dto.UnitCost.HasValue
            ? product.StockQuantity + dto.Quantity
            : product.StockQuantity - dto.Quantity;

        return newStock < 0
            ? (nameof(dto.Quantity), $"Stock insuficiente para el ajuste. Disponible: {product.StockQuantity}")
            : null;
    }
}

/// <summary>
/// Validador para filtros de kardex
/// </summary>
public class StockMovementFilterValidator : AbstractValidator<StockMovementFilterDto>
{
    public StockMovementFilterValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("La página debe ser mayor a 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100");

        RuleFor(x => x)
            .Must(x => !x.DateFrom.HasValue || !x.DateTo.HasValue || x.DateFrom <= x.DateTo)
            .WithMessage("La fecha desde no puede ser mayor a la fecha hasta");
    }
}