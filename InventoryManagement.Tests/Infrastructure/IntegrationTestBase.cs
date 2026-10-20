using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using InventoryManagement.Api.Common;
using InventoryManagement.Api.DTOs;

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

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }
}
