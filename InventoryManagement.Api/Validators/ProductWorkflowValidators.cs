using FluentValidation;
using InventoryManagement.Api.DTOs;

namespace InventoryManagement.Api.Validators;

/// <summary>
/// Validador para aprobar un producto (Admin define el precio de venta)
/// </summary>
public class ApproveProductValidator : AbstractValidator<ApproveProductDto>
{
    public ApproveProductValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0")
            .LessThanOrEqualTo(999999.99m).WithMessage("El precio no puede exceder 999,999.99");
    }
}

/// <summary>
/// Validador del motivo (rechazo de producto o retiro de la venta)
/// </summary>
public class ReasonValidator : AbstractValidator<ReasonDto>
{
    public ReasonValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("El motivo es obligatorio")
            .MaximumLength(500).WithMessage("El motivo no puede exceder 500 caracteres");
    }
}

/// <summary>
/// Validador del comentario de Admin al revisar una solicitud de cambio
/// </summary>
public class ReviewChangeRequestValidator : AbstractValidator<ReviewChangeRequestDto>
{
    public ReviewChangeRequestValidator()
    {
        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("El comentario no puede exceder 500 caracteres")
            .When(x => !string.IsNullOrEmpty(x.Comment));
    }
}
