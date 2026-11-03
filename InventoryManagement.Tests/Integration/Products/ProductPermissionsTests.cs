using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.Products;

/// <summary>
/// Contador lee el catálogo (para abrir el kardex valorizado) pero no puede modificarlo.
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

    private Task<HttpClient> CreateContadorClientAsync()
        => CreateClientForRoleAsync(UserRole.Contador, "contador.test", "22.222.222-2");
}
