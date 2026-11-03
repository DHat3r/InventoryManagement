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
            .Map(dest => dest.ProductsCount, src => src.Products != null ? src.Products.Count : 0)
            .Map(dest => dest.ParentCategoryId, src => src.ParentCategoryId)
            .Map(dest => dest.ParentCategoryName, src => src.ParentCategory != null ? src.ParentCategory.Name : null);

        // Category -> CategoryTreeDto (no recursive - se construye manualmente en endpoint)
        TypeAdapterConfig<Category, CategoryTreeDto>
            .NewConfig()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.Children, src => new List<CategoryTreeDto>());

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
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.Products)
            .Ignore(dest => dest.ParentCategory!)
            .Ignore(dest => dest.ChildCategories);

        // UpdateCategoryDto -> Category (para actualización parcial)
        TypeAdapterConfig<UpdateCategoryDto, Category>
            .NewConfig()
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Products)
            .Ignore(dest => dest.ParentCategory!)
            .Ignore(dest => dest.ChildCategories);

        // Product -> ProductDto (completo)
        TypeAdapterConfig<Product, ProductDto>
            .NewConfig()
            .Map(dest => dest.Category, src => MapCategoryRef(src.Category))
            .Map(dest => dest.SKU, src => src.SKU)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsLowStock, src => src.StockQuantity <= src.MinStockLevel)
            // Disponibilidad y solicitudes pendientes se calculan, no se guardan (no pueden desincronizarse)
            .Map(dest => dest.IsOutOfStock, src => src.StockQuantity == 0)
            .Map(dest => dest.IsPossiblyUnavailable, src => src.ChangeRequests
                .Any(r => r.Type == ChangeRequestType.Retiro && r.Status == ChangeRequestStatus.Pendiente))
            .Map(dest => dest.HasPendingEdit, src => src.ChangeRequests
                .Any(r => r.Type == ChangeRequestType.Edicion && r.Status == ChangeRequestStatus.Pendiente))
            .Map(dest => dest.MarginPercentage, src =>
                src.Price.HasValue && src.Cost > 0
                    ? Math.Round(((src.Price.Value - src.Cost) / src.Cost) * 100, 2)
                    : (decimal?)null);

        // Product -> ProductListDto (ligero para listados)
        TypeAdapterConfig<Product, ProductListDto>
            .NewConfig()
            .Map(dest => dest.Category, src => MapCategoryRef(src.Category))
            .Map(dest => dest.SKU, src => src.SKU)
            .Map(dest => dest.IsLowStock, src => src.StockQuantity <= src.MinStockLevel)
            .Map(dest => dest.IsOutOfStock, src => src.StockQuantity == 0)
            .Map(dest => dest.IsPossiblyUnavailable, src => src.ChangeRequests
                .Any(r => r.Type == ChangeRequestType.Retiro && r.Status == ChangeRequestStatus.Pendiente))
            .Map(dest => dest.HasPendingEdit, src => src.ChangeRequests
                .Any(r => r.Type == ChangeRequestType.Edicion && r.Status == ChangeRequestStatus.Pendiente));

        // ProductChangeRequest -> ProductChangeRequestDto (con los valores actuales para comparar)
        TypeAdapterConfig<ProductChangeRequest, ProductChangeRequestDto>
            .NewConfig()
            .Map(dest => dest.ProductName, src => src.Product.Name)
            .Map(dest => dest.Current, src => new ProductCurrentValuesDto(
                src.Product.Name, src.Product.SKU, src.Product.Description, src.Product.Cost,
                src.Product.CategoryId, src.Product.MinStockLevel, src.Product.UnitOfMeasure));

        // CreateProductDto -> Product (auto-mapping + explicit para SKU y otros)
        TypeAdapterConfig<CreateProductDto, Product>
            .NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.SKU, src => src.SKU)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.StockQuantity, src => src.StockQuantity)
            .Map(dest => dest.MinStockLevel, src => src.MinStockLevel)
            .Map(dest => dest.UnitOfMeasure, src => src.UnitOfMeasure)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UpdatedAt!)
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
            .Ignore(nameof(Product.Price)) // null = "no cambiar": lo resuelve el endpoint
            .Ignore(dest => dest.Category);

        // UpdateStockDto -> Product (parcial)
        TypeAdapterConfig<UpdateStockDto, Product>
            .NewConfig()
            .Map(dest => dest.StockQuantity, src => src.Quantity)
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Name)
            .Ignore(dest => dest.SKU!)
            .Ignore(dest => dest.Description!)
            .Ignore(nameof(Product.Price))
            .Ignore(dest => dest.Cost)
            .Ignore(dest => dest.MinStockLevel)
            .Ignore(dest => dest.UnitOfMeasure!)
            .Ignore(dest => dest.CategoryId)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.Category);

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

        // User -> UserInfoDto
        TypeAdapterConfig<User, UserInfoDto>
            .NewConfig()
            .Map(dest => dest.Rut, src => src.Rut);

        // User -> UserListDto
        TypeAdapterConfig<User, UserListDto>
            .NewConfig()
            .Map(dest => dest.Rut, src => src.Rut);

        // CreateUserDto -> User
        TypeAdapterConfig<CreateUserDto, User>
            .NewConfig()
            .Map(dest => dest.CreatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.PasswordHash, src => src.Password) // Se hashea en el servicio
            .Map(dest => dest.Rut, src => NormalizeRut(src.Rut))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.UpdatedAt!)
            .Ignore(dest => dest.RefreshTokens)
            .Ignore(dest => dest.StockMovements)
            .Ignore(dest => dest.CategoriesCreated)
            .Ignore(dest => dest.CategoriesModified)
            .Ignore(dest => dest.ProductsCreated)
            .Ignore(dest => dest.ProductsModified);

        // UpdateUserDto -> User
        TypeAdapterConfig<UpdateUserDto, User>
            .NewConfig()
            .Map(dest => dest.UpdatedAt, _ => DateTime.UtcNow)
            .Map(dest => dest.Rut, src => NormalizeRut(src.Rut))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedAt)
            .Ignore(dest => dest.PasswordHash)
            .Ignore(dest => dest.RefreshTokens)
            .Ignore(dest => dest.StockMovements)
            .Ignore(dest => dest.CategoriesCreated)
            .Ignore(dest => dest.CategoriesModified)
            .Ignore(dest => dest.ProductsCreated)
            .Ignore(dest => dest.ProductsModified);
    }

    private static CategoryRefDto MapCategoryRef(Category? category)
    {
        return category != null
            ? new CategoryRefDto(category.Id, category.Name)
            : new CategoryRefDto(0, string.Empty);
    }

    private static string NormalizeRut(string? rut)
        => string.IsNullOrWhiteSpace(rut) ? null! : rut.Replace(".", "").Replace("-", "").ToUpper();
}