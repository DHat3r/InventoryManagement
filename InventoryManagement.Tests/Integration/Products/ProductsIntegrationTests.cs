using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.Models;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.Products;

public class ProductsIntegrationTests : IntegrationTestBase
{
    public ProductsIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithDateFilters_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/products?CreatedAtFrom=2026-10-19");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithMultiSort_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/products?SortBy=price:desc,name:asc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_ValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateProductDto(
            Name: "Test Product",
            Price: 100.00m,
            Cost: 50.00m,
            CategoryId: 1,
            SKU: "TEST-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Description: "Test product",
            StockQuantity: 10,
            MinStockLevel: 5,
            UnitOfMeasure: "UN"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/products", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetProducts_WithDateRange_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/products?CreatedAtFrom=2026-10-19&CreatedAtTo=2026-10-20");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetProducts_WithMultiSort_ReturnsSortedResults()
    {
        // Act
        var response = await Client.GetAsync("/api/products?SortBy=price:desc,name:asc");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ProductListDto>>>(JsonOptions);
        result!.Data!.Data.Should().NotBeEmpty();

        // Verify sorting: price descending, then name ascending
        var products = result!.Data!.Data;
        for (int i = 1; i < products.Count; i++)
        {
            var current = products[i];
            var previous = products[i - 1];

            if (previous.Price != current.Price)
            {
                previous.Price.Should().BeGreaterThanOrEqualTo(current.Price);
            }
            else
            {
                string.Compare(previous.Name, current.Name, StringComparison.Ordinal).Should().BeLessThanOrEqualTo(0);
            }
        }
    }
}
