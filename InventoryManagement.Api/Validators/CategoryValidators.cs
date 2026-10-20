using FluentValidation;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Validators;

/// <summary>
/// Validador para crear categoría
/// </summary>
public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    private readonly InventoryDbContext _db;

    public CreateCategoryValidator(InventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .Matches(@"^[\p{L}\p{N}\s\-']+$").WithMessage("El nombre solo puede contener letras, números, espacios, guiones y apóstrofes");

        RuleFor(x => x.Name)
            .MustAsync(NameUnique).WithMessage("Ya existe una categoría con este nombre")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ParentCategoryId)
            .MustAsync(ParentCategoryExists).WithMessage("La categoría padre no existe")
            .When(x => x.ParentCategoryId.HasValue);
    }

    private async Task<bool> ParentCategoryExists(int? parentCategoryId, CancellationToken ct)
    {
        if (!parentCategoryId.HasValue) return true;
        return await _db.Categories.AnyAsync(c => c.Id == parentCategoryId.Value && c.IsActive, ct);
    }

    private async Task<bool> NameUnique(string name, CancellationToken ct)
    {
        var normalized = name.Trim().ToLower();
        return !await _db.Categories.AnyAsync(c => c.Name.ToLower() == normalized, ct);
    }
}

/// <summary>
/// Validador para actualizar categoría
/// </summary>
public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    private readonly InventoryDbContext _db;
    private int? _categoryId;

    public UpdateCategoryValidator(InventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .Matches(@"^[\p{L}\p{N}\s\-']+$").WithMessage("El nombre solo puede contener letras, números, espacios, guiones y apóstrofes");

        RuleFor(x => x.Name)
            .MustAsync(NameUniqueForUpdate).WithMessage("Ya existe una categoría con este nombre")
            .When(x => !string.IsNullOrWhiteSpace(x.Name) && _categoryId.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ParentCategoryId)
            .MustAsync(ParentCategoryExists).WithMessage("La categoría padre no existe")
            .When(x => x.ParentCategoryId.HasValue);

        RuleFor(x => x)
            .MustAsync(NotCircularReference).WithMessage("No se puede asignar una categoría como padre de sí misma o de sus descendientes")
            .When(x => x.ParentCategoryId.HasValue);
    }

    public UpdateCategoryValidator SetCategoryId(int categoryId)
    {
        _categoryId = categoryId;
        return this;
    }

    private async Task<bool> ParentCategoryExists(int? parentCategoryId, CancellationToken ct)
    {
        if (!parentCategoryId.HasValue) return true;
        return await _db.Categories.AnyAsync(c => c.Id == parentCategoryId.Value && c.IsActive, ct);
    }

    private async Task<bool> NameUniqueForUpdate(string name, CancellationToken ct)
    {
        var normalized = name.Trim().ToLower();
        return !await _db.Categories.AnyAsync(c =>
            c.Name.ToLower() == normalized && c.Id != _categoryId!.Value, ct);
    }

    private async Task<bool> NotCircularReference(UpdateCategoryDto dto, CancellationToken ct)
    {
        if (!_categoryId.HasValue || !dto.ParentCategoryId.HasValue) return true;
        
        // No puede ser padre de sí misma
        if (dto.ParentCategoryId == _categoryId) return false;
        
        // Verificar que no sea descendiente (evitar ciclos)
        var currentParentId = dto.ParentCategoryId;
        while (currentParentId.HasValue)
        {
            if (currentParentId == _categoryId) return false;
            var parent = await _db.Categories.FirstOrDefaultAsync(c => c.Id == currentParentId.Value, ct);
            currentParentId = parent?.ParentCategoryId;
        }
        return true;
    }
}