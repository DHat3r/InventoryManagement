using InventoryManagement.Api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace InventoryManagement.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory for integration tests with TestContainers PostgreSQL.
/// The container is started in <see cref="InitializeAsync"/> (xUnit class fixture lifecycle)
/// and the DbContext is pointed at it during host configuration.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer;

    public TestWebApplicationFactory()
    {
        _postgreSqlContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("inventory_test_db")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove the DbContext registrations pointing to the real PostgreSQL instance
            var dbContextDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<InventoryDbContext>)
                         || d.ServiceType == typeof(DbContextOptions))
                .ToList();

            foreach (var descriptor in dbContextDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(_postgreSqlContainer.GetConnectionString()));

            // Create schema + seed data. This runs while the host is being built,
            // before Program.cs executes the admin seed on startup.
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            db.Database.Migrate();
        });
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
    }
}
