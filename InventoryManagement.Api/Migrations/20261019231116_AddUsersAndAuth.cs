using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InventoryManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "stock_movements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "categories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModifiedBy",
                table: "categories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rut = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<byte>(type: "smallint", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1668), null, null });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1669), null, null });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1670), null, null });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1671), null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1810), null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1812), null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1814), null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1815), null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1817), null, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "CreatedBy", "ModifiedBy" },
                values: new object[] { new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1818), null, null });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_UserId",
                table: "stock_movements",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_products_CreatedBy",
                table: "products",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_products_ModifiedBy",
                table: "products",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_categories_CreatedBy",
                table: "categories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_categories_ModifiedBy",
                table: "categories",
                column: "ModifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId_RevokedAt",
                table: "refresh_tokens",
                columns: new[] { "UserId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Role_IsActive",
                table: "users",
                columns: new[] { "Role", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_users_Rut",
                table: "users",
                column: "Rut",
                unique: true,
                filter: "\"Rut\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_users_UserName",
                table: "users",
                column: "UserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_categories_users_CreatedBy",
                table: "categories",
                column: "CreatedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_categories_users_ModifiedBy",
                table: "categories",
                column: "ModifiedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_products_users_CreatedBy",
                table: "products",
                column: "CreatedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_products_users_ModifiedBy",
                table: "products",
                column: "ModifiedBy",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_stock_movements_users_UserId",
                table: "stock_movements",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_users_CreatedBy",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_categories_users_ModifiedBy",
                table: "categories");

            migrationBuilder.DropForeignKey(
                name: "FK_products_users_CreatedBy",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_products_users_ModifiedBy",
                table: "products");

            migrationBuilder.DropForeignKey(
                name: "FK_stock_movements_users_UserId",
                table: "stock_movements");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_UserId",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_products_CreatedBy",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_ModifiedBy",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_categories_CreatedBy",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_ModifiedBy",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "categories");

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6667));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6668));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6669));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6670));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6832));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6833));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6835));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6836));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6838));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 21, 47, 48, 616, DateTimeKind.Utc).AddTicks(6839));
        }
    }
}
