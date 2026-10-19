using FluentValidation;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Api.Validators;

/// <summary>
/// Validador para crear categoría
/// </summary>
public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .Matches(@"^[\p{L}\p{N}\s\-']+$").WithMessage("El nombre solo puede contener letras, números, espacios, guiones y apóstrofes");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

/// <summary>
/// Validador para actualizar categoría
/// </summary>
public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres")
            .Matches(@"^[\p{L}\p{N}\s\-']+$").WithMessage("El nombre solo puede contener letras, números, espacios, guiones y apóstrofes");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}