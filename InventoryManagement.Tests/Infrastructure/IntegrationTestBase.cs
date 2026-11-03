using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests. Creates a client and authenticates it as the seeded admin.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    /// <summary>
    /// The API serializes enums as strings (JsonStringEnumConverter), so tests must do the same.
    /// </summary>
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected TestWebApplicationFactory Factory { get; }
    protected HttpClient Client { get; private set; } = null!;

    protected IntegrationTestBase(TestWebApplicationFactory factory)
    {
        Factory = factory;
    }

    public async Task InitializeAsync()
    {
        Client = Factory.CreateClient();

        var response = await Client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(UserName: "admin", Password: "Admin123!"));

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(JsonOptions);
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", result!.Data!.AccessToken);
    }

    /// <summary>
    /// Crea (si no existe) un usuario con el rol indicado y devuelve un cliente autenticado como él.
    /// Si el usuario ya existe (otro test de la clase lo creó), la API responde 400 y se ignora.
    /// </summary>
    protected async Task<HttpClient> CreateClientForRoleAsync(UserRole role, string userName, string rut)
    {
        const string password = "Rol12345!";
        await Client.PostAsJsonAsync("/api/users", new CreateUserDto(
            UserName: userName,
            Password: password,
            FullName: $"Usuario {role}",
            Rut: rut,
            Role: role), JsonOptions);

        var client = Factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginDto(userName, password));
        login.EnsureSuccessStatusCode();
        var result = await login.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Data!.AccessToken);
        return client;
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
