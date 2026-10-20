using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Common;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.Auth;

public class AuthIntegrationTests : IntegrationTestBase
{
    public AuthIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var dto = new LoginDto(
            UserName: "admin",
            Password: "Admin123!"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(JsonOptions);
        result!.Data.Should().NotBeNull();
        result!.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result!.Data!.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto(
            UserName: "admin",
            Password: "wrongpassword"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange - Login first
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto(
            UserName: "admin",
            Password: "Admin123!"
        ));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(JsonOptions);
        var refreshToken = loginResult!.Data!.RefreshToken;

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenDto(
            RefreshToken: refreshToken
        ));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RefreshResponseDto>>(JsonOptions);
        result!.Data.Should().NotBeNull();
        result!.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result!.Data!.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Logout_ValidToken_ReturnsOk()
    {
        // Arrange - Login first
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto(
            UserName: "admin",
            Password: "Admin123!"
        ));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>(JsonOptions);
        var refreshToken = loginResult!.Data!.RefreshToken;

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenDto(
            RefreshToken: refreshToken
        ));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
