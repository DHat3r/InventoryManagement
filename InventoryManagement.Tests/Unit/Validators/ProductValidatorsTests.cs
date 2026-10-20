using FluentAssertions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Api.Validators;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Tests.Unit.Validators;

public class ProductValidatorsTests
{
    private static InventoryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new InventoryDbContext(options);
    }

    private static async Task<InventoryDbContext> CreateDbContextWithCategoryAsync(int categoryId = 1)
    {
        var db = CreateDbContext();
        db.Categories.Add(new Category
        {
            Id = categoryId,
            Name = "Electrónica",
            Description = "Categoría de prueba",
            IsActive = true
        });
        await db.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task CreateProductValidator_ValidDto_ReturnsSuccess()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync();
        var validator = new CreateProductValidator(db);
        var dto = new CreateProductDto(
            Name: "Laptop Gaming",
            Price: 1299.99m,
            Cost: 950.00m,
            CategoryId: 1,
            SKU: "LAP-GAM-001",
            Description: "Laptop para gaming",
            StockQuantity: 15,
            MinStockLevel: 5,
            UnitOfMeasure: "UN"
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProductValidator_InvalidSKU_ReturnsFailure()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync();
        var validator = new CreateProductValidator(db);
        var dto = new CreateProductDto(
            Name: "Test",
            Price: 100,
            Cost: 50,
            CategoryId: 1,
            SKU: "invalid-sku",
            Description: null,
            StockQuantity: 0,
            MinStockLevel: 10,
            UnitOfMeasure: "UN"
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU"
            && e.ErrorMessage.Contains("letras mayúsculas, números y guiones"));
    }

    [Fact]
    public async Task CreateProductValidator_DuplicateSKU_ReturnsFailure()
    {
        // Arrange - seed the product in the SAME context used by the validator
        await using var db = await CreateDbContextWithCategoryAsync();
        db.Products.Add(new Product
        {
            Id = 1,
            Name = "Producto existente",
            SKU = "LAP-GAM-001",
            Price = 100,
            Cost = 50,
            CategoryId = 1
        });
        await db.SaveChangesAsync();

        var validator = new CreateProductValidator(db);
        var dto = new CreateProductDto(
            Name: "Test",
            Price: 100,
            Cost: 50,
            CategoryId: 1,
            SKU: "LAP-GAM-001",
            Description: null,
            StockQuantity: 0,
            MinStockLevel: 10,
            UnitOfMeasure: "UN"
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SKU" && e.ErrorMessage.Contains("Ya existe"));
    }

    [Fact]
    public async Task CreateProductValidator_PriceZero_ReturnsFailure()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync();
        var validator = new CreateProductValidator(db);
        var dto = new CreateProductDto(
            Name: "Test",
            Price: 0,
            Cost: 50,
            CategoryId: 1,
            SKU: null,
            Description: null,
            StockQuantity: 0,
            MinStockLevel: 10,
            UnitOfMeasure: "UN"
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price" && e.ErrorMessage.Contains("mayor a 0"));
    }

    [Fact]
    public async Task CreateProductValidator_CostNegative_ReturnsFailure()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync();
        var validator = new CreateProductValidator(db);
        var dto = new CreateProductDto(
            Name: "Test",
            Price: 100,
            Cost: -10,
            CategoryId: 1,
            SKU: null,
            Description: null,
            StockQuantity: 0,
            MinStockLevel: 10,
            UnitOfMeasure: "UN"
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Cost" && e.ErrorMessage.Contains("no puede ser negativo"));
    }

    [Fact]
    public async Task CreateProductValidator_NegativeStock_ReturnsFailure()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync();
        var validator = new CreateProductValidator(db);
        var dto = new CreateProductDto(
            Name: "Test",
            Price: 100,
            Cost: 50,
            CategoryId: 1,
            SKU: null,
            Description: null,
            StockQuantity: -5,
            MinStockLevel: 10,
            UnitOfMeasure: "UN"
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "StockQuantity" && e.ErrorMessage.Contains("no puede ser negativo"));
    }

    [Fact]
    public async Task UpdateProductValidator_ValidDto_ReturnsSuccess()
    {
        // Arrange
        await using var db = await CreateDbContextWithCategoryAsync();
        var validator = new UpdateProductValidator(db);
        validator.SetProductId(1);
        var dto = new UpdateProductDto(
            Name: "Laptop Gaming Pro",
            Price: 1399.99m,
            Cost: 1000.00m,
            CategoryId: 1,
            SKU: "LAP-GAM-002",
            Description: "Laptop mejorada",
            StockQuantity: 20,
            MinStockLevel: 5,
            UnitOfMeasure: "UN",
            IsActive: true
        );

        // Act
        var result = await validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
