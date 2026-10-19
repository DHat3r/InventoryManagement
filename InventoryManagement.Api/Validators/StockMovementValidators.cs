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

        // Validaciones de negocio según tipo
        RuleFor(x => x)
            .MustAsync(ValidateBusinessRules).WithMessage("Validación de negocio fallida");
    }

    public CreateStockMovementValidator SetProductId(int productId)
    {
        _productId = productId;
        return this;
    }

    private async Task<bool> ValidateBusinessRules(CreateStockMovementDto dto, CancellationToken ct)
    {
        if (_productId <= 0) return false;

        var product = await _db.Products
            .Where(p => p.Id == _productId && p.IsActive)
            .FirstOrDefaultAsync(ct);

        if (product is null) return false;

        return dto.Type switch
        {
            MovementType.Entrada => ValidateEntrada(dto, product),
            MovementType.Salida => ValidateSalida(dto, product),
            MovementType.Ajuste => ValidateAjuste(dto, product),
            _ => false
        };
    }

    private bool ValidateEntrada(CreateStockMovementDto dto, Product product)
    {
        // Entrada: Quantity > 0, UnitCost requerido (para valoración)
        if (!dto.UnitCost.HasValue) return false;
        return true;
    }

    private bool ValidateSalida(CreateStockMovementDto dto, Product product)
    {
        // Salida: Quantity > 0, stock suficiente
        if (product.StockQuantity < dto.Quantity) return false;
        // UnitCost opcional para salida (se usa costo promedio actual)
        return true;
    }

    private bool ValidateAjuste(CreateStockMovementDto dto, Product product)
    {
        // Ajuste: Quantity > 0, stock final no negativo
        // Para ajustes, Quantity puede representar aumento o disminución
        // Usamos UnitCost para saber dirección: si tiene UnitCost = entrada, si no = salida
        // Pero mejor: validamos que stock final >= 0
        var newStock = dto.UnitCost.HasValue
            ? product.StockQuantity + dto.Quantity  // Ajuste positivo (tiene costo = entrada)
            : product.StockQuantity - dto.Quantity; // Ajuste negativo (sin costo = salida)

        if (newStock < 0) return false;
        return true;
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