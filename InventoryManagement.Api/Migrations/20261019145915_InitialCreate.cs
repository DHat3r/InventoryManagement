using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InventoryManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    MinStockLevel = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValue: "UN"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_products_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2243), "Productos electrónicos y tecnológicos", true, "Electrónica", null },
                    { 2, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2245), "Prendas de vestir y accesorios", true, "Ropa", null },
                    { 3, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2248), "Artículos para el hogar y decoración", true, "Hogar", null },
                    { 4, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2250), "Equipamiento y ropa deportiva", true, "Deportes", null }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "CategoryId", "Cost", "CreatedAt", "Description", "IsActive", "MinStockLevel", "Name", "Price", "SKU", "StockQuantity", "UnitOfMeasure", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1, 950.00m, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2467), "Laptop para gaming de alto rendimiento", true, 5, "Laptop Gaming", 1299.99m, "LAP-GAM-001", 15, "UN", null },
                    { 2, 1, 25.00m, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2470), "Mouse ergonómico inalámbrico", true, 10, "Mouse Inalámbrico", 49.99m, "MOU-WLS-001", 50, "UN", null },
                    { 3, 1, 45.00m, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2473), "Teclado mecánico RGB", true, 8, "Teclado Mecánico", 89.99m, "KEY-MEC-001", 30, "UN", null },
                    { 4, 4, 12.00m, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2475), "Camiseta transpirable para deporte", true, 20, "Camiseta Deportiva", 24.99m, "TSH-SPT-001", 100, "UN", null },
                    { 5, 4, 65.00m, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2477), "Zapatillas para running profesionales", true, 10, "Zapatillas Running", 119.99m, "SHO-RUN-001", 25, "PAR", null },
                    { 6, 3, 18.00m, new DateTime(2026, 10, 19, 14, 59, 15, 671, DateTimeKind.Utc).AddTicks(2480), "Lámpara de escritorio LED regulable", true, 10, "Lámpara LED", 34.99m, "LAM-LED-001", 40, "UN", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_categories_Name",
                table: "categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_CategoryId",
                table: "products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_products_Name",
                table: "products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_products_SKU",
                table: "products",
                column: "SKU",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}
