using FluentValidation;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Validators;

/// <summary>
/// Validador para crear producto
/// </summary>
public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    private readonly InventoryDbContext _db;

    public CreateProductValidator(InventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.SKU)
            .MaximumLength(50).WithMessage("El SKU no puede exceder 50 caracteres")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("El SKU solo puede contener letras mayúsculas, números y guiones")
            .When(x => !string.IsNullOrEmpty(x.SKU));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("El precio no puede exceder 999,999.99");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo")
            .LessThanOrEqualTo(999999.99m).WithMessage("El costo no puede exceder 999,999.99");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("El nivel mínimo de stock no puede ser negativo");

        RuleFor(x => x.UnitOfMeasure)
            .MaximumLength(50).WithMessage("La unidad de medida no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.UnitOfMeasure));

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida")
            .MustAsync(CategoryExists).WithMessage("La categoría seleccionada no existe");

        RuleFor(x => x.SKU)
            .MustAsync(SkuUnique).WithMessage("Ya existe un producto con este SKU")
            .When(x => !string.IsNullOrEmpty(x.SKU));
    }

    private async Task<bool> CategoryExists(int categoryId, CancellationToken ct)
    {
        return await _db.Categories.AnyAsync(c => c.Id == categoryId, ct);
    }

    private async Task<bool> SkuUnique(string? sku, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(sku)) return true;
        return !await _db.Products.AnyAsync(p => p.SKU == sku, ct);
    }
}

/// <summary>
/// Validador para actualizar producto
/// </summary>
public class UpdateProductValidator : AbstractValidator<UpdateProductDto>
{
    private readonly InventoryDbContext _db;
    private int? _productId;

    public UpdateProductValidator(InventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres");

        RuleFor(x => x.SKU)
            .MaximumLength(50).WithMessage("El SKU no puede exceder 50 caracteres")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("El SKU solo puede contener letras mayúsculas, números y guiones")
            .When(x => !string.IsNullOrEmpty(x.SKU));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("La descripción no puede exceder 1000 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("El precio no puede exceder 999,999.99");

        RuleFor(x => x.Cost)
            .GreaterThanOrEqualTo(0).WithMessage("El costo no puede ser negativo")
            .LessThanOrEqualTo(999999.99m).WithMessage("El costo no puede exceder 999,999.99");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo");

        RuleFor(x => x.MinStockLevel)
            .GreaterThanOrEqualTo(0).WithMessage("El nivel mínimo de stock no puede ser negativo");

        RuleFor(x => x.UnitOfMeasure)
            .MaximumLength(50).WithMessage("La unidad de medida no puede exceder 50 caracteres")
            .When(x => !string.IsNullOrEmpty(x.UnitOfMeasure));

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Debe seleccionar una categoría válida")
            .MustAsync(CategoryExists).WithMessage("La categoría seleccionada no existe");

        RuleFor(x => x.SKU)
            .MustAsync(SkuUniqueForUpdate).WithMessage("Ya existe otro producto con este SKU")
            .When(x => !string.IsNullOrEmpty(x.SKU));
    }

    public UpdateProductValidator SetProductId(int productId)
    {
        _productId = productId;
        return this;
    }

    private async Task<bool> CategoryExists(int categoryId, CancellationToken ct)
    {
        return await _db.Categories.AnyAsync(c => c.Id == categoryId, ct);
    }

    private async Task<bool> SkuUniqueForUpdate(string? sku, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(sku) || _productId == null) return true;
        return !await _db.Products.AnyAsync(p => p.SKU == sku && p.Id != _productId.Value, ct);
    }
}

/// <summary>
/// Validador para actualización de stock
/// </summary>
public class UpdateStockValidator : AbstractValidator<UpdateStockDto>
{
    public UpdateStockValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("La cantidad no puede ser negativa")
            .LessThanOrEqualTo(999999).WithMessage("La cantidad no puede exceder 999,999");
    }
}

/// <summary>
/// Validador para filtros de productos
/// </summary>
public class ProductFilterValidator : AbstractValidator<ProductFilterDto>
{
    public ProductFilterValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("La página debe ser mayor a 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio mínimo no puede ser negativo")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0).WithMessage("El precio máximo no puede ser negativo")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("El precio mínimo no puede ser mayor al precio máximo");
    }
}