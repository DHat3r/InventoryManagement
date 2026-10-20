using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.Models;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.StockMovements;

public class StockMovementsIntegrationTests : IntegrationTestBase
{
    public StockMovementsIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateMovement_Entrada_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateStockMovementDto(
            Type: MovementType.Entrada,
            Quantity: 10,
            Reason: "Test Entrada",
            Reference: "TEST-001",
            UnitCost: 100.00m
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/products/1/movements", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateMovement_Salida_InsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateStockMovementDto(
            Type: MovementType.Salida,
            Quantity: 999999, // More than available stock
            Reason: "Test Salida",
            Reference: "TEST-001",
            UnitCost: null
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/products/1/movements", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetKardex_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/products/1/kardex");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetKardexSummary_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/products/1/kardex/summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
