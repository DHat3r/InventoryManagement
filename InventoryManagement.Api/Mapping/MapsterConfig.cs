using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using Mapster;

namespace InventoryManagement.Api.Mapping;

/// <summary>
/// Configuración de Mapster para mapeos Entity ↔ DTO
/// </summary>
public static class MapsterConfig
{
    public static void RegisterMappings()
    {
        // Category -> CategoryDto
        TypeAdapterConfig<Category, CategoryDto>
            .NewConfig()
            .Map(dest => dest.ProductsCount, src => src.Products != null ? src.Products.Count : 0);

        // Category -> CategoryRefDto (ligero)
        TypeAdapterConfig<Category, CategoryRefDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name);

        // CreateCategoryDto -> Category
        TypeAdapterConfig<CreateCategoryDto, Category>
            .NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.IsActive, _ => true)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Products);

        // UpdateCategoryDto -> Category (para actualización parcial)
        TypeAdapterConfig<UpdateCategoryDto, Category>
            .NewConfig()
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Products);

        // Product -> ProductDto (completo)
        TypeAdapterConfig<Product, ProductDto>
            .NewConfig()
            .Map(dest => dest.Category, src => MapCategoryRef(src.Category))
            .Map(dest => dest.SKU, src => src.SKU)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsLowStock, src => src.StockQuantity <= src.MinStockLevel)
            .Map(dest => dest.MarginPercentage, src => 
                src.Cost > 0 ? Math.Round(((src.Price - src.Cost) / src.Cost) * 100, 2) : 0);

        // Product -> ProductListDto (ligero para listados)
        TypeAdapterConfig<Product, ProductListDto>
            .NewConfig()
            .Map(dest => dest.Category, src => MapCategoryRef(src.Category))
            .Map(dest => dest.SKU, src => src.SKU)
            .Map(dest => dest.IsLowStock, src => src.StockQuantity <= src.MinStockLevel);

        // CreateProductDto -> Product (auto-mapping + explicit para SKU y otros)
        TypeAdapterConfig<CreateProductDto, Product>
            .NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.IsActive, _ => true)
            .Map(dest => dest.SKU, src => src.SKU)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.StockQuantity, src => src.StockQuantity)
            .Map(dest => dest.MinStockLevel, src => src.MinStockLevel)
            .Map(dest => dest.UnitOfMeasure, src => src.UnitOfMeasure)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UpdatedAt)
            .Ignore(dest => dest.Category);

        // UpdateProductDto -> Product (auto-mapping + explicit)
        TypeAdapterConfig<UpdateProductDto, Product>
            .NewConfig()
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.SKU, src => src.SKU)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.StockQuantity, src => src.StockQuantity)
            .Map(dest => dest.MinStockLevel, src => src.MinStockLevel)
            .Map(dest => dest.UnitOfMeasure, src => src.UnitOfMeasure)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Category);

        // UpdateStockDto -> Product (parcial)
        TypeAdapterConfig<UpdateStockDto, Product>
            .NewConfig()
            .Map(dest => dest.StockQuantity, src => src.Quantity)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Name)
            .Ignore(dest => dest.SKU)
            .Ignore(dest => dest.Description)
            .Ignore(dest => dest.Price)
            .Ignore(dest => dest.Cost)
            .Ignore(dest => dest.MinStockLevel)
            .Ignore(dest => dest.UnitOfMeasure)
            .Ignore(dest => dest.IsActive)
            .Ignore(dest => dest.CategoryId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Category);

        // ============================================================
        // STOCK MOVEMENTS (Fase 3)
        // ============================================================

        // StockMovement -> StockMovementDto (completo)
        TypeAdapterConfig<StockMovement, StockMovementDto>
            .NewConfig()
            .Map(dest => dest.ProductId, src => src.ProductId);

        // StockMovement -> StockMovementListDto (ligero para listados)
        TypeAdapterConfig<StockMovement, StockMovementListDto>
            .NewConfig();

        // CreateStockMovementDto -> StockMovement (crear entidad)
        TypeAdapterConfig<CreateStockMovementDto, StockMovement>
            .NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ProductId)
            .Ignore(dest => dest.Product)
            .Ignore(dest => dest.PreviousStock)
            .Ignore(dest => dest.NewStock);
    }

    private static CategoryRefDto MapCategoryRef(Category? category)
    {
        return category != null 
            ? new CategoryRefDto(category.Id, category.Name) 
            : new CategoryRefDto(0, string.Empty);
    }
}