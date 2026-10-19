using FluentValidation;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Validators;

/// <summary>
/// Validador para login
/// </summary>
public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede exceder 50 caracteres");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MaximumLength(100).WithMessage("La contraseña no puede exceder 100 caracteres");
    }
}

/// <summary>
/// Validador para crear usuario (Admin)
/// </summary>
public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    private readonly InventoryDbContext _db;

    public CreateUserValidator(InventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede exceder 50 caracteres")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("El nombre de usuario solo puede contener letras, números, punto, guion y guion bajo")
            .MustAsync(UserNameUnique).WithMessage("El nombre de usuario ya existe");

        RuleFor(x => x.Rut)
            .Must(BeValidRut).When(x => !string.IsNullOrEmpty(x.Rut))
            .WithMessage("El RUT no tiene un formato válido (ej: 12.345.678-9)")
            .MustAsync(RutUnique).When(x => !string.IsNullOrEmpty(x.Rut))
            .WithMessage("El RUT ya está registrado");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula")
            .Matches(@"[a-z]").WithMessage("La contraseña debe contener al menos una minúscula")
            .Matches(@"\d").WithMessage("La contraseña debe contener al menos un número")
            .Matches(@"[\W_]").WithMessage("La contraseña debe contener al menos un símbolo");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("El nombre completo es obligatorio")
            .MaximumLength(200).WithMessage("El nombre completo no puede exceder 200 caracteres");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Rol inválido");

        // Validación condicional: RUT obligatorio para Admin, Inventario, Contador
        RuleFor(x => x.Rut)
            .NotEmpty().When(x => x.Role != UserRole.Comprador)
            .WithMessage("El RUT es obligatorio para Admin, Inventario y Contador");
    }

    private async Task<bool> UserNameUnique(string userName, CancellationToken ct)
        => !await _db.Users.AnyAsync(u => u.UserName == userName, ct);

    private async Task<bool> RutUnique(string? rut, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rut)) return true;
        var normalized = NormalizeRut(rut);
        return !await _db.Users.AnyAsync(u => u.Rut == normalized, ct);
    }

    private bool BeValidRut(string? rut)
    {
        if (string.IsNullOrEmpty(rut)) return true;
        return ValidateChileanRut(rut);
    }

    private static string NormalizeRut(string rut)
        => rut.Replace(".", "").Replace("-", "").ToUpper();

    private static bool ValidateChileanRut(string rut)
    {
        var clean = NormalizeRut(rut);
        if (clean.Length < 2) return false;

        var body = clean[..^1];
        var dv = clean[^1];

        if (!long.TryParse(body, out _)) return false;
        if (dv != 'K' && !char.IsDigit(dv)) return false;

        // Algoritmo módulo 11
        int sum = 0, multiplier = 2;
        for (int i = body.Length - 1; i >= 0; i--)
        {
            sum += (body[i] - '0') * multiplier;
            multiplier = multiplier == 7 ? 2 : multiplier + 1;
        }
        var remainder = sum % 11;
        var expectedDv = remainder switch
        {
            0 => '0',
            1 => 'K',
            _ => char.Parse((11 - remainder).ToString())
        };

        return dv == expectedDv;
    }
}

/// <summary>
/// Validador para actualizar usuario (Admin)
/// </summary>
public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    private readonly InventoryDbContext _db;
    private int? _userId;

    public UpdateUserValidator(InventoryDbContext db)
    {
        _db = db;

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio")
            .MaximumLength(50).WithMessage("El nombre de usuario no puede exceder 50 caracteres")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("El nombre de usuario solo puede contener letras, números, punto, guion y guion bajo")
            .MustAsync(UserNameUnique).WithMessage("El nombre de usuario ya existe");

        RuleFor(x => x.Rut)
            .Must(BeValidRut).When(x => !string.IsNullOrEmpty(x.Rut))
            .WithMessage("El RUT no tiene un formato válido (ej: 12.345.678-9)")
            .MustAsync(RutUnique).When(x => !string.IsNullOrEmpty(x.Rut))
            .WithMessage("El RUT ya está registrado");

        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("El nombre completo no puede exceder 200 caracteres")
            .When(x => !string.IsNullOrEmpty(x.FullName));

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Rol inválido");

        // RUT obligatorio para roles que no sean Comprador
        RuleFor(x => x.Rut)
            .NotEmpty().When(x => x.Role != UserRole.Comprador)
            .WithMessage("El RUT es obligatorio para Admin, Inventario y Contador");
    }

    public UpdateUserValidator SetUserId(int userId)
    {
        _userId = userId;
        return this;
    }

    private async Task<bool> UserNameUnique(string userName, CancellationToken ct)
    {
        if (_userId == null) return !await _db.Users.AnyAsync(u => u.UserName == userName, ct);
        return !await _db.Users.AnyAsync(u => u.UserName == userName && u.Id != _userId.Value, ct);
    }

    private async Task<bool> RutUnique(string? rut, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(rut)) return true;
        var normalized = NormalizeRut(rut);
        if (_userId == null) return !await _db.Users.AnyAsync(u => u.Rut == normalized, ct);
        return !await _db.Users.AnyAsync(u => u.Rut == normalized && u.Id != _userId.Value, ct);
    }

    private bool BeValidRut(string? rut)
    {
        if (string.IsNullOrEmpty(rut)) return true;
        return ValidateChileanRut(rut);
    }

    private static string NormalizeRut(string rut)
        => rut.Replace(".", "").Replace("-", "").ToUpper();

    private static bool ValidateChileanRut(string rut)
    {
        var clean = NormalizeRut(rut);
        if (clean.Length < 2) return false;

        var body = clean[..^1];
        var dv = clean[^1];

        if (!long.TryParse(body, out _)) return false;
        if (dv != 'K' && !char.IsDigit(dv)) return false;

        int sum = 0, multiplier = 2;
        for (int i = body.Length - 1; i >= 0; i--)
        {
            sum += (body[i] - '0') * multiplier;
            multiplier = multiplier == 7 ? 2 : multiplier + 1;
        }
        var remainder = sum % 11;
        var expectedDv = remainder switch
        {
            0 => '0',
            1 => 'K',
            _ => char.Parse((11 - remainder).ToString())
        };

        return dv == expectedDv;
    }
}

/// <summary>
/// Validador para cambiar password
/// </summary>
public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres")
            .Matches(@"[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula")
            .Matches(@"[a-z]").WithMessage("La contraseña debe contener al menos una minúscula")
            .Matches(@"\d").WithMessage("La contraseña debe contener al menos un número")
            .Matches(@"[\W_]").WithMessage("La contraseña debe contener al menos un símbolo");

        RuleFor(x => x)
            .Must(x => x.CurrentPassword != x.NewPassword)
            .WithMessage("La nueva contraseña debe ser diferente a la actual")
            .When(x => !string.IsNullOrEmpty(x.CurrentPassword) && !string.IsNullOrEmpty(x.NewPassword));
    }
}

/// <summary>
/// Validador para refresh token
/// </summary>
public class RefreshTokenValidator : AbstractValidator<RefreshTokenDto>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("El refresh token es obligatorio");
    }
}

/// <summary>
/// Validador para filtros de usuarios
/// </summary>
public class UserFilterValidator : AbstractValidator<UserFilterDto>
{
    public UserFilterValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("La página debe ser mayor a 0");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100");

        RuleFor(x => x.Role)
            .IsInEnum().When(x => x.Role.HasValue)
            .WithMessage("Rol inválido");
    }
}