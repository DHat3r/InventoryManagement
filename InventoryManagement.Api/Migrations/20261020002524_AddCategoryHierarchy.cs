using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCategoryId",
                table: "categories",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ParentCategoryId" },
                values: new object[] { new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3176), null });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ParentCategoryId" },
                values: new object[] { new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3178), null });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ParentCategoryId" },
                values: new object[] { new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3179), null });

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ParentCategoryId" },
                values: new object[] { new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3180), null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3313));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3315));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3316));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3318));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3319));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 20, 0, 25, 23, 877, DateTimeKind.Utc).AddTicks(3321));

            migrationBuilder.CreateIndex(
                name: "IX_categories_ParentCategoryId",
                table: "categories",
                column: "ParentCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_categories_categories_ParentCategoryId",
                table: "categories",
                column: "ParentCategoryId",
                principalTable: "categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_categories_categories_ParentCategoryId",
                table: "categories");

            migrationBuilder.DropIndex(
                name: "IX_categories_ParentCategoryId",
                table: "categories");

            migrationBuilder.DropColumn(
                name: "ParentCategoryId",
                table: "categories");

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1668));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1669));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1670));

            migrationBuilder.UpdateData(
                table: "categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1671));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1810));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1812));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1814));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1815));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1817));

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 19, 23, 11, 16, 459, DateTimeKind.Utc).AddTicks(1818));
        }
    }
}
