using FluentAssertions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Api.Validators;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Tests.Unit.Validators;

public class CategoryValidatorsTests
{
    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    private static async Task<InventoryDbContext> CreateDbContextWithCategoryAsync(
        int id = 1,
        string name = "Electrónica",
        int? parentCategoryId = null)
    {
        var db = CreateDbContext();
        db.Categories.Add(new Category
        {
            Id = id,
            Name = name,
            Description = "Categoría existente",
            IsActive = true,
            ParentCategoryId = parentCategoryId
        });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task CreateCategoryValidator_ValidDto_ReturnsSuccess()
    {
        // Arrange
        await using var db = CreateDbContext();
        var validator = new CreateCategoryValidator(db);
        var dto = new CreateCategoryDto(
            Name: "Tecnología",
            Description: "Productos tecnológicos",
            ParentCategoryId: null
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCategoryValidator_EmptyName_ReturnsFailure()
    {
        // Arrange
        await using var db = CreateDbContext();
        var validator = new CreateCategoryValidator(db);
        var dto = new CreateCategoryDto(
            Name: "",
            Description: "Test",
            ParentCategoryId: null
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("obligatorio"));
    }

    [Fact]
    public async Task CreateCategoryValidator_NameTooLong_ReturnsFailure()
    {
        // Arrange
        await using var db = CreateDbContext();
        var validator = new CreateCategoryValidator(db);
        var dto = new CreateCategoryDto(
            Name: new string('a', 101),
            Description: "Test",
            ParentCategoryId: null
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && e.ErrorMessage.Contains("100 caracteres"));
    }

    [Fact]
    public async Task CreateCategoryValidator_InvalidCharactersInName_ReturnsFailure()
    {
        // Arrange
        await using var db = CreateDbContext();
        var validator = new CreateCategoryValidator(db);
        var dto = new CreateCategoryDto(
            Name: "Electrónica@#$",
            Description: "Test",
            ParentCategoryId: null
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name"
            && e.ErrorMessage.Contains("letras, números, espacios, guiones y apóstrofes"));
    }

    [Fact]
    public async Task CreateCategoryValidator_DescriptionTooLong_ReturnsFailure()
    {
        // Arrange
        await using var db = CreateDbContext();
        var validator = new CreateCategoryValidator(db);
        var dto = new CreateCategoryDto(
            Name: "Test",
            Description: new string('a', 501),
            ParentCategoryId: null
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && e.ErrorMessage.Contains("500 caracteres"));
    }

    [Fact]
    public async Task CreateCategoryValidator_DuplicateName_ReturnsFailure()
    {
        // Arrange - seed the category in the SAME context used by the validator
        await using var db = await CreateDbContextWithCategoryAsync(name: "Electrónica");
        var validator = new CreateCategoryValidator(db);
        var dto = new CreateCategoryDto(
            Name: "Electrónica",
            Description: "Test",
            ParentCategoryId: null
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Ya existe"));
    }

    [Fact]
    public async Task UpdateCategoryValidator_ValidDto_ReturnsSuccess()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync(id: 1, name: "Electrónica");
        var validator = new UpdateCategoryValidator(db);
        validator.SetCategoryId(1);
        var dto = new UpdateCategoryDto(
            Name: "Hogar",
            Description: "Nueva descripción",
            IsActive: true,
            ParentCategoryId: null
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCategoryValidator_CircularReference_ReturnsFailure()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync(id: 1);
        var validator = new UpdateCategoryValidator(db);
        validator.SetCategoryId(1);
        var dto = new UpdateCategoryDto(
            Name: "Test",
            Description: null,
            IsActive: true,
            ParentCategoryId: 1
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("padre de sí misma"));
    }
}
