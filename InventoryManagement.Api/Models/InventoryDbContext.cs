using Microsoft.EntityFrameworkCore;
using InventoryManagement.Api.Models;

namespace InventoryManagement.Api.Models;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Audit FKs (nullable, SET NULL)
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CategoriesCreated)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ModifiedByUser)
                  .WithMany(u => u.CategoriesModified)
                  .HasForeignKey(e => e.ModifiedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SKU).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Cost).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
            entity.Property(e => e.MinStockLevel).HasDefaultValue(10);
            entity.Property(e => e.UnitOfMeasure).HasMaxLength(50).HasDefaultValue("UN");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // Audit FKs (nullable, SET NULL)
            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.ProductsCreated)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(e => e.ModifiedByUser)
                  .WithMany(u => u.ProductsModified)
                  .HasForeignKey(e => e.ModifiedBy)
                  .OnDelete(DeleteBehavior.SetNull);
            
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.HasIndex(e => e.Name);
            
            // Relationship
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // StockMovement configuration (Fase 3 - Kardex)
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("stock_movements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).HasConversion<byte>().IsRequired();
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Reason).HasMaxLength(200);
            entity.Property(e => e.Reference).HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PreviousStock).IsRequired();
            entity.Property(e => e.NewStock).IsRequired();
            
            // Indexes for kardex queries
            entity.HasIndex(e => new { e.ProductId, e.CreatedAt });
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Reference);
            
            // Relationships
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // User FK (nullable, SET NULL para auditoría)
            entity.HasOne(e => e.User)
                  .WithMany(u => u.StockMovements)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // User configuration (Fase 4 - Auth)
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserName).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Rut).HasMaxLength(12);
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Role).HasConversion<byte>().IsRequired();
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.FailedLoginAttempts).HasDefaultValue(0);
            
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.Rut).IsUnique().HasFilter("\"Rut\" IS NOT NULL");
            entity.HasIndex(e => new { e.Role, e.IsActive });
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.ReplacedByTokenHash).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.HasIndex(e => new { e.UserId, e.RevokedAt });
            
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electrónica", Description = "Productos electrónicos y tecnológicos", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Ropa", Description = "Prendas de vestir y accesorios", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 3, Name = "Hogar", Description = "Artículos para el hogar y decoración", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 4, Name = "Deportes", Description = "Equipamiento y ropa deportiva", IsActive = true, CreatedAt = DateTime.UtcNow }
        );

        // Seed Products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop Gaming", SKU = "LAP-GAM-001", Description = "Laptop para gaming de alto rendimiento", Price = 1299.99m, Cost = 950.00m, StockQuantity = 15, MinStockLevel = 5, UnitOfMeasure = "UN", CategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Mouse Inalámbrico", SKU = "MOU-WLS-001", Description = "Mouse ergonómico inalámbrico", Price = 49.99m, Cost = 25.00m, StockQuantity = 50, MinStockLevel = 10, UnitOfMeasure = "UN", CategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "Teclado Mecánico", SKU = "KEY-MEC-001", Description = "Teclado mecánico RGB", Price = 89.99m, Cost = 45.00m, StockQuantity = 30, MinStockLevel = 8, UnitOfMeasure = "UN", CategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 4, Name = "Camiseta Deportiva", SKU = "TSH-SPT-001", Description = "Camiseta transpirable para deporte", Price = 24.99m, Cost = 12.00m, StockQuantity = 100, MinStockLevel = 20, UnitOfMeasure = "UN", CategoryId = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 5, Name = "Zapatillas Running", SKU = "SHO-RUN-001", Description = "Zapatillas para running profesionales", Price = 119.99m, Cost = 65.00m, StockQuantity = 25, MinStockLevel = 10, UnitOfMeasure = "PAR", CategoryId = 4, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 6, Name = "Lámpara LED", SKU = "LAM-LED-001", Description = "Lámpara de escritorio LED regulable", Price = 34.99m, Cost = 18.00m, StockQuantity = 40, MinStockLevel = 10, UnitOfMeasure = "UN", CategoryId = 3, IsActive = true, CreatedAt = DateTime.UtcNow }
        );
    }
}