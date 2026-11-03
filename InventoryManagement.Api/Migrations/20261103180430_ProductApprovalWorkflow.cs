using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InventoryManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProductApprovalWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "products",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<byte>(
                name: "ApprovalStatus",
                table: "products",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)2); // productos existentes: Aprobado (ya se vendían)

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Lifecycle",
                table: "products",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)1); // productos existentes: Activo...

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "product_change_requests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Status = table.Column<byte>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: true),
                    MinStockLevel = table.Column<int>(type: "integer", nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedBy = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ReviewedBy = table.Column<int>(type: "integer", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewComment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_change_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_change_requests_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "Lifecycle", "RejectionReason" },
                values: new object[] { (byte)2, null, null, (byte)1, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "Lifecycle", "RejectionReason" },
                values: new object[] { (byte)2, null, null, (byte)1, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "Lifecycle", "RejectionReason" },
                values: new object[] { (byte)2, null, null, (byte)1, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "Lifecycle", "RejectionReason" },
                values: new object[] { (byte)2, null, null, (byte)1, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "Lifecycle", "RejectionReason" },
                values: new object[] { (byte)2, null, null, (byte)1, null });

            migrationBuilder.UpdateData(
                table: "products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "ApprovalStatus", "ApprovedAt", "ApprovedBy", "Lifecycle", "RejectionReason" },
                values: new object[] { (byte)2, null, null, (byte)1, null });

            // ...salvo los que estaban desactivados (IsActive = false): pasan a Descontinuado.
            // Va DESPUÉS de los UpdateData de la semilla (que ponen Lifecycle = 1) y ANTES de borrar IsActive.
            migrationBuilder.Sql("UPDATE products SET \"Lifecycle\" = 3 WHERE \"IsActive\" = false;");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "products");

            migrationBuilder.CreateIndex(
                name: "IX_products_ApprovalStatus",
                table: "products",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_product_change_requests_ProductId_Type_Status",
                table: "product_change_requests",
                columns: new[] { "ProductId", "Type", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_product_change_requests_Status",
                table: "product_change_requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_change_requests");

            // Volver atrás sin perder datos: primero se reconstruye IsActive desde Lifecycle
            // (solo Activo cuenta como activo) y se rellena el precio de los pendientes.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql("UPDATE products SET \"IsActive\" = (\"Lifecycle\" = 1);");
            migrationBuilder.Sql("UPDATE products SET \"Price\" = 0 WHERE \"Price\" IS NULL;");

            migrationBuilder.DropIndex(
                name: "IX_products_ApprovalStatus",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Lifecycle",
                table: "products");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "products");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                table: "products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);
        }
    }
}
