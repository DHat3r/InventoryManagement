using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.Products;

/// <summary>
/// Contador lee el catálogo (para abrir el kardex valorizado) pero no puede modificarlo.
/// Inventario ve costos (valor de ingreso) pero no el precio de venta ni el margen.
/// </summary>
public class ProductPermissionsTests : IntegrationTestBase
{
    public ProductPermissionsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("/api/products")]
    [InlineData("/api/products/1")]
    [InlineData("/api/products/low-stock")]
    [InlineData("/api/products/1/kardex/summary")]
    public async Task Contador_ReadEndpoints_ReturnOk(string url)
    {
        // Arrange
        using var contador = await CreateContadorClientAsync();

        // Act
        var response = await contador.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Contador_CreateProduct_ReturnsForbidden()
    {
        // Arrange
        using var contador = await CreateContadorClientAsync();
        var dto = new CreateProductDto(Name: "Producto de Contador", Price: 10m, Cost: 5m, CategoryId: 1, SKU: "CNT-001");

        // Act
        var response = await contador.PostAsJsonAsync("/api/products", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Inventario_GetProductById_HidesSalePriceButShowsCost()
    {
        // Arrange
        using var inventario = await CreateInventarioClientAsync();

        // Act
        var response = await inventario.GetAsync("/api/products/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = (await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>(JsonOptions))!.Data!;
        product.Price.Should().BeNull();
        product.MarginPercentage.Should().BeNull();
        product.Cost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Inventario_GetProducts_HidesSalePrice()
    {
        // Arrange
        using var inventario = await CreateInventarioClientAsync();

        // Act
        var response = await inventario.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = (await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductListDto>>>(JsonOptions))!.Data!;
        page.Data.Should().NotBeEmpty();
        page.Data.Should().OnlyContain(p => p.Price == null);
    }

    [Fact]
    public async Task Inventario_PatchStock_ResponseHidesSalePrice()
    {
        // Arrange
        using var inventario = await CreateInventarioClientAsync();

        // Act
        var response = await inventario.PatchAsJsonAsync("/api/products/2/stock", new UpdateStockDto(Quantity: 50));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = (await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>(JsonOptions))!.Data!;
        product.Price.Should().BeNull();
    }

    // Ordenar o acotar por precio permitiría deducirlo aunque la respuesta no lo traiga
    [Theory]
    [InlineData("/api/products?sortBy=price:desc")]
    [InlineData("/api/products?minPrice=100")]
    [InlineData("/api/products?maxPrice=100")]
    public async Task Inventario_FilterOrSortByPrice_ReturnsForbidden(string url)
    {
        // Arrange
        using var inventario = await CreateInventarioClientAsync();

        // Act
        var response = await inventario.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Contador_GetProductById_ShowsSalePrice()
    {
        // Arrange
        using var contador = await CreateContadorClientAsync();

        // Act
        var response = await contador.GetAsync("/api/products/1");

        // Assert
        var product = (await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>(JsonOptions))!.Data!;
        product.Price.Should().NotBeNull();
        product.MarginPercentage.Should().NotBeNull();
    }

    private Task<HttpClient> CreateContadorClientAsync()
        => CreateClientForRoleAsync(UserRole.Contador, "contador.test", "22.222.222-2");

    private Task<HttpClient> CreateInventarioClientAsync()
        => CreateClientForRoleAsync(UserRole.Inventario, "inventario.test", "11.111.111-1");
}
