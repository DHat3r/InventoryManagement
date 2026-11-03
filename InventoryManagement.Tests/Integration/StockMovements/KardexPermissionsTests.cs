using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.StockMovements;

/// <summary>
/// Inventario ve el kardex con el costo de ingreso de cada movimiento, pero no el resumen valorizado (contable).
/// </summary>
public class KardexPermissionsTests : IntegrationTestBase
{
    public KardexPermissionsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Inventario_GetKardex_ReturnsHistoryWithUnitCost()
    {
        // Arrange: el admin registra una entrada con costo para que haya al menos un movimiento valorizado
        var entrada = new CreateStockMovementDto(MovementType.Entrada, 3, "Test permisos", "PERM-001", 100.00m);
        (await Client.PostAsJsonAsync("/api/products/1/movements", entrada)).StatusCode.Should().Be(HttpStatusCode.Created);
        using var inventario = await CreateInventarioClientAsync();

        // Act
        var response = await inventario.GetAsync("/api/products/1/kardex");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<StockMovementListDto>>>(JsonOptions);
        result!.Data!.Data.Should().NotBeEmpty();
        result.Data.Data.Should().Contain(m => m.UnitCost == 100.00m);
    }

    [Fact]
    public async Task Inventario_GetKardexSummary_ReturnsForbidden()
    {
        // Arrange
        using var inventario = await CreateInventarioClientAsync();

        // Act
        var response = await inventario.GetAsync("/api/products/1/kardex/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_GetKardex_IncludesUnitCost()
    {
        // Arrange
        var entrada = new CreateStockMovementDto(MovementType.Entrada, 2, "Test permisos", "PERM-002", 50.00m);
        (await Client.PostAsJsonAsync("/api/products/1/movements", entrada)).StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        var response = await Client.GetAsync("/api/products/1/kardex");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<StockMovementListDto>>>(JsonOptions);
        result!.Data!.Data.Should().Contain(m => m.UnitCost != null);
    }

    private Task<HttpClient> CreateInventarioClientAsync()
        => CreateClientForRoleAsync(UserRole.Inventario, "inventario.test", "11.111.111-1");
}
