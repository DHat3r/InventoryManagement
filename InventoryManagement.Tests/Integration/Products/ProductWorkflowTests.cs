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
/// Flujo de aprobación y vigencia: Inventario propone (alta sin precio, ediciones, retiros) y Admin aprueba.
/// Cada test crea su propio producto para no depender de los demás.
/// </summary>
public class ProductWorkflowTests : IntegrationTestBase
{
    public ProductWorkflowTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    // ─── Alta ───

    [Fact]
    public async Task Inventario_CreateProduct_WithoutPrice_StaysPending()
    {
        using var inventario = await InventarioAsync();

        var product = await CreateProductAsync(inventario, price: null);

        product.ApprovalStatus.Should().Be(ProductApprovalStatus.Pendiente);
        product.Price.Should().BeNull();
    }

    [Fact]
    public async Task Inventario_CreateProduct_WithPrice_ReturnsBadRequest()
    {
        using var inventario = await InventarioAsync();

        var response = await inventario.PostAsJsonAsync("/api/products", NewProduct(price: 10m));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_CreateProduct_WithoutPrice_ReturnsBadRequest()
    {
        var response = await Client.PostAsJsonAsync("/api/products", NewProduct(price: null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_CreateProduct_WithPrice_IsApproved()
    {
        var product = await CreateProductAsync(Client, price: 20m);

        product.ApprovalStatus.Should().Be(ProductApprovalStatus.Aprobado);
        product.Price.Should().Be(20m);
    }

    // ─── Aprobación ───

    [Fact]
    public async Task PendingProduct_AcceptsEntradaButNotSalida()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(inventario, price: null);

        var entrada = await inventario.PostAsJsonAsync($"/api/products/{product.Id}/movements",
            new CreateStockMovementDto(MovementType.Entrada, 10, "Llegó la mercadería", null, 5m));
        var salida = await inventario.PostAsJsonAsync($"/api/products/{product.Id}/movements",
            new CreateStockMovementDto(MovementType.Salida, 1));

        entrada.StatusCode.Should().Be(HttpStatusCode.Created);
        salida.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await salida.Content.ReadAsStringAsync()).Should().Contain("pendiente de aprobación");
    }

    [Fact]
    public async Task Admin_ApprovePendingProduct_SetsPriceAndApproves()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(inventario, price: null);

        var response = await Client.PostAsJsonAsync($"/api/products/{product.Id}/approve", new ApproveProductDto(15m));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var approved = await ReadAsync<ProductDto>(response);
        approved.ApprovalStatus.Should().Be(ProductApprovalStatus.Aprobado);
        approved.Price.Should().Be(15m);
    }

    [Fact]
    public async Task Admin_ApproveAlreadyApprovedProduct_ReturnsConflict()
    {
        var product = await CreateProductAsync(Client, price: 20m);

        var response = await Client.PostAsJsonAsync($"/api/products/{product.Id}/approve", new ApproveProductDto(25m));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Inventario_CannotApprove()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(inventario, price: null);

        var response = await inventario.PostAsJsonAsync($"/api/products/{product.Id}/approve", new ApproveProductDto(15m));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RejectedProduct_EditedByInventario_GoesBackToPending()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(inventario, price: null);
        (await Client.PostAsJsonAsync($"/api/products/{product.Id}/reject", new ReasonDto("Falta descripción")))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await inventario.PutAsJsonAsync($"/api/products/{product.Id}", UpdateFrom(product, "Con descripción"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync<ProductDto>(response)).ApprovalStatus.Should().Be(ProductApprovalStatus.Pendiente);
    }

    // ─── Ediciones propuestas ───

    [Fact]
    public async Task Inventario_EditApprovedProduct_CreatesRequestWithoutChangingProduct()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(Client, price: 20m);

        var response = await inventario.PutAsJsonAsync($"/api/products/{product.Id}", UpdateFrom(product, "Nombre propuesto"));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var current = await GetProductAsync(product.Id);
        current.Name.Should().Be(product.Name);
        current.HasPendingEdit.Should().BeTrue();
    }

    [Fact]
    public async Task Inventario_SecondEditWhilePending_ReturnsConflict()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(Client, price: 20m);
        await inventario.PutAsJsonAsync($"/api/products/{product.Id}", UpdateFrom(product, "Primera propuesta"));

        var response = await inventario.PutAsJsonAsync($"/api/products/{product.Id}", UpdateFrom(product, "Segunda propuesta"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Inventario_EditWithPrice_ReturnsBadRequest()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(Client, price: 20m);

        var response = await inventario.PutAsJsonAsync($"/api/products/{product.Id}", UpdateFrom(product, "Con precio") with { Price = 99m });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_ApproveEditRequest_AppliesProposedValues()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(Client, price: 20m);
        var requestId = await ProposeEditAsync(inventario, product, "Nombre aprobado");

        var response = await Client.PostAsJsonAsync($"/api/product-change-requests/{requestId}/approve", new ReviewChangeRequestDto());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var current = await GetProductAsync(product.Id);
        current.Name.Should().Be("Nombre aprobado");
        current.HasPendingEdit.Should().BeFalse();
        current.Price.Should().Be(20m, "una edición de Inventario nunca toca el precio");
    }

    [Fact]
    public async Task Admin_RejectEditRequest_KeepsProductUnchanged()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(Client, price: 20m);
        var requestId = await ProposeEditAsync(inventario, product, "Nombre rechazado");

        var response = await Client.PostAsJsonAsync($"/api/product-change-requests/{requestId}/reject", new ReviewChangeRequestDto("No corresponde"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetProductAsync(product.Id)).Name.Should().Be(product.Name);
    }

    [Fact]
    public async Task Admin_ListsPendingRequests_WithCurrentValues()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductAsync(Client, price: 20m);
        var requestId = await ProposeEditAsync(inventario, product, "Nombre en la bandeja");

        var response = await Client.GetAsync("/api/product-change-requests");

        var list = await ReadAsync<List<ProductChangeRequestDto>>(response);
        var request = list.Should().ContainSingle(r => r.Id == requestId).Subject;
        request.Name.Should().Be("Nombre en la bandeja");
        request.SKU.Should().Be(product.SKU, "un campo propuesto igual al actual no debe verse como borrado");
        request.Current.Name.Should().Be(product.Name);
    }

    // ─── Retiro y vigencia ───

    [Fact]
    public async Task Inventario_RequestWithdrawal_BlocksSalidaUntilAdminDecides()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductWithStockAsync();

        var withdraw = await inventario.PostAsJsonAsync($"/api/products/{product.Id}/withdraw", new ReasonDto("Lote con falla"));
        var salida = await SalidaAsync(product.Id);

        withdraw.StatusCode.Should().Be(HttpStatusCode.Accepted);
        (await GetProductAsync(product.Id)).IsPossiblyUnavailable.Should().BeTrue();
        salida.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await salida.Content.ReadAsStringAsync()).Should().Contain("posiblemente no disponible");
    }

    [Fact]
    public async Task WithdrawalApproved_ProductRetiradoUntilReactivated()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductWithStockAsync();
        var withdraw = await inventario.PostAsJsonAsync($"/api/products/{product.Id}/withdraw", new ReasonDto("Recall del proveedor"));
        var requestId = (await ReadAsync<ProductChangeRequestDto>(withdraw)).Id;

        await Client.PostAsJsonAsync($"/api/product-change-requests/{requestId}/approve", new ReviewChangeRequestDto());
        var retirado = await GetProductAsync(product.Id);
        var salidaRetirado = await SalidaAsync(product.Id);
        var reactivate = await Client.PostAsync($"/api/products/{product.Id}/reactivate", null);
        var salidaActivo = await SalidaAsync(product.Id);

        retirado.Lifecycle.Should().Be(ProductLifecycle.Retirado);
        retirado.IsPossiblyUnavailable.Should().BeFalse();
        salidaRetirado.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        reactivate.StatusCode.Should().Be(HttpStatusCode.OK);
        salidaActivo.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task WithdrawalRejected_SalidaAllowedAgain()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductWithStockAsync();
        var withdraw = await inventario.PostAsJsonAsync($"/api/products/{product.Id}/withdraw", new ReasonDto("Falsa alarma"));
        var requestId = (await ReadAsync<ProductChangeRequestDto>(withdraw)).Id;

        await Client.PostAsJsonAsync($"/api/product-change-requests/{requestId}/reject", new ReviewChangeRequestDto("Revisado, está bien"));
        var salida = await SalidaAsync(product.Id);

        salida.StatusCode.Should().Be(HttpStatusCode.Created);
        (await GetProductAsync(product.Id)).Lifecycle.Should().Be(ProductLifecycle.Activo);
    }

    [Fact]
    public async Task Admin_Withdraw_IsImmediate()
    {
        var product = await CreateProductWithStockAsync();

        var response = await Client.PostAsJsonAsync($"/api/products/{product.Id}/withdraw", new ReasonDto("Lote dañado"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync<ProductDto>(response)).Lifecycle.Should().Be(ProductLifecycle.Retirado);
    }

    [Fact]
    public async Task Inventario_CannotReactivate()
    {
        using var inventario = await InventarioAsync();
        var product = await CreateProductWithStockAsync();
        await Client.PostAsJsonAsync($"/api/products/{product.Id}/withdraw", new ReasonDto("Lote dañado"));

        var response = await inventario.PostAsync($"/api/products/{product.Id}/reactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DiscontinuedProduct_RejectsEntrada()
    {
        var product = await CreateProductAsync(Client, price: 20m);
        (await Client.DeleteAsync($"/api/products/{product.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var entrada = await Client.PostAsJsonAsync($"/api/products/{product.Id}/movements",
            new CreateStockMovementDto(MovementType.Entrada, 1, null, null, 5m));

        entrada.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await entrada.Content.ReadAsStringAsync()).Should().Contain("descontinuado");
    }

    [Fact]
    public async Task OutOfStockProduct_IsFlagged()
    {
        var product = await CreateProductAsync(Client, price: 20m);

        product.StockQuantity.Should().Be(0);
        product.IsOutOfStock.Should().BeTrue();
    }

    // ─── Helpers ───

    private Task<HttpClient> InventarioAsync()
        => CreateClientForRoleAsync(UserRole.Inventario, "inventario.test", "11.111.111-1");

    private static CreateProductDto NewProduct(decimal? price)
        => new(Name: $"Producto {Guid.NewGuid():N}"[..20], Price: price, Cost: 5m, CategoryId: 1,
               SKU: $"WF-{Guid.NewGuid():N}"[..15].ToUpperInvariant()); // el SKU solo acepta mayúsculas

    private static UpdateProductDto UpdateFrom(ProductDto product, string newName)
        => new(Name: newName, Price: null, Cost: product.Cost, CategoryId: product.Category.Id,
               SKU: product.SKU, Description: product.Description, StockQuantity: product.StockQuantity,
               MinStockLevel: product.MinStockLevel, UnitOfMeasure: product.UnitOfMeasure);

    private static async Task<ProductDto> CreateProductAsync(HttpClient client, decimal? price)
    {
        var response = await client.PostAsJsonAsync("/api/products", NewProduct(price));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadAsync<ProductDto>(response);
    }

    // Producto aprobado (Admin) con 10 unidades, para probar salidas
    private async Task<ProductDto> CreateProductWithStockAsync()
    {
        var product = await CreateProductAsync(Client, price: 20m);
        (await Client.PostAsJsonAsync($"/api/products/{product.Id}/movements",
            new CreateStockMovementDto(MovementType.Entrada, 10, null, null, 5m))).StatusCode.Should().Be(HttpStatusCode.Created);
        return product;
    }

    private async Task<int> ProposeEditAsync(HttpClient inventario, ProductDto product, string newName)
    {
        var response = await inventario.PutAsJsonAsync($"/api/products/{product.Id}", UpdateFrom(product, newName));
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        return (await ReadAsync<ProductChangeRequestDto>(response)).Id;
    }

    private Task<HttpResponseMessage> SalidaAsync(int productId)
        => Client.PostAsJsonAsync($"/api/products/{productId}/movements", new CreateStockMovementDto(MovementType.Salida, 1));

    private async Task<ProductDto> GetProductAsync(int id)
        => await ReadAsync<ProductDto>(await Client.GetAsync($"/api/products/{id}"));

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
        => (await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions))!.Data!;
}
