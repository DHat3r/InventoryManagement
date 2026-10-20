using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.Common;
using InventoryManagement.Tests.Infrastructure;
using Xunit;

namespace InventoryManagement.Tests.Integration.Categories;

public class CategoriesIntegrationTests : IntegrationTestBase
{
    public CategoriesIntegrationTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetCategories_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCategoriesTree_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/categories/tree");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCategory_ValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateCategoryDto(
            Name: "Test Category",
            Description: "Test Description",
            ParentCategoryId: null
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/categories", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCategory_WithParentCategory_ReturnsCreated()
    {
        // Arrange - First create parent
        var parentDto = new CreateCategoryDto(
            Name: "Parent Category",
            Description: "Parent Description",
            ParentCategoryId: null
        );

        var parentResponse = await Client.PostAsJsonAsync("/api/categories", parentDto);
        parentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parentResult = await parentResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>(JsonOptions);
        var parentId = parentResult!.Data!.Id;

        // Act - Create child
        var childDto = new CreateCategoryDto(
            Name: "Child Category",
            Description: "Child Description",
            ParentCategoryId: parentId
        );

        var response = await Client.PostAsJsonAsync("/api/categories", childDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetCategoriesTree_ReturnsHierarchy()
    {
        // Arrange - Create parent and child
        var parentDto = new CreateCategoryDto(
            Name: "Tree Parent " + Guid.NewGuid().ToString("N")[..8],
            Description: "Parent for tree test",
            ParentCategoryId: null
        );

        var parentResponse = await Client.PostAsJsonAsync("/api/categories", parentDto);
        parentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var parentResult = await parentResponse.Content.ReadFromJsonAsync<ApiResponse<CategoryDto>>(JsonOptions);
        var parentId = parentResult!.Data!.Id;

        await Client.PostAsJsonAsync("/api/categories", new CreateCategoryDto(
            Name: "Child Tree " + Guid.NewGuid().ToString("N")[..8],
            Description: "Child for tree test",
            ParentCategoryId: parentId
        ));

        // Act
        var response = await Client.GetAsync("/api/categories/tree");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateCategory_DuplicateName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateCategoryDto(
            Name: "Unique Category " + Guid.NewGuid().ToString("N")[..8],
            Description: "Test",
            ParentCategoryId: null
        );

        // Create first
        var firstResponse = await Client.PostAsJsonAsync("/api/categories", dto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Try duplicate
        var response = await Client.PostAsJsonAsync("/api/categories", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
