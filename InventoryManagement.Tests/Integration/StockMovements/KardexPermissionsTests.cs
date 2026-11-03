using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.StockMovements;

/// <summary>
/// Inventario ve el kardex de stock (cantidades) pero no los valores: ni el resumen valorizado ni el costo unitario.
/// </summary>
public class KardexPermissionsTests : IntegrationTestBase
{
    private const string InventarioUserName = "inventario.test";
    private const string InventarioPassword = "Inventario123!";

    public KardexPermissionsTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Inventario_GetKardex_ReturnsHistoryWithoutUnitCost()
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
        result.Data.Data.Should().OnlyContain(m => m.UnitCost == null);
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

    /// <summary>
    /// Crea (si no existe) un usuario con rol Inventario y devuelve un cliente autenticado como él.
    /// </summary>
    private async Task<HttpClient> CreateInventarioClientAsync()
    {
        // Si ya existe (otro test de la clase lo creó), la API responde 400 y se ignora
        await Client.PostAsJsonAsync("/api/users", new CreateUserDto(
            UserName: InventarioUserName,
            Password: InventarioPassword,
            FullName: "Usuario de Inventario",
            Rut: "11.111.111-1",
            Role: UserRole.Inventario), JsonOptions);

        var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(InventarioUserName, InventarioPassword));
        login.EnsureSuccessStatusCode();
        var result = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Data!.AccessToken);
        return client;
    }
}
