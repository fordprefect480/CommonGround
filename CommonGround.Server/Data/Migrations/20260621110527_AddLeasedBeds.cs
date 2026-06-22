using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeasedBeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LeasedBedPriceCents",
                table: "SiteSettings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "MembershipPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BedRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ResolvedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BedRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BedRequests_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Section = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BedLeases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BedId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PriceAtAllocationCents = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BedLeases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BedLeases_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BedLeases_Beds_BedId",
                        column: x => x.BedId,
                        principalTable: "Beds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BedLeasePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BedLeaseId = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    StripeCheckoutSessionId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AmountCents = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BedLeasePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BedLeasePayments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BedLeasePayments_BedLeases_BedLeaseId",
                        column: x => x.BedLeaseId,
                        principalTable: "BedLeases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Beds",
                columns: new[] { "Id", "IsActive", "Label", "Notes", "Number", "Section" },
                values: new object[,]
                {
                    { 1, true, null, null, 1, "N" },
                    { 2, true, null, null, 2, "N" },
                    { 3, true, null, null, 3, "N" },
                    { 4, true, null, null, 4, "N" },
                    { 5, true, null, null, 5, "N" },
                    { 6, true, null, null, 6, "N" },
                    { 7, true, null, null, 7, "N" },
                    { 8, true, null, null, 8, "N" },
                    { 9, true, null, null, 9, "N" },
                    { 10, true, null, null, 10, "N" },
                    { 11, true, null, null, 11, "N" },
                    { 12, true, null, null, 12, "N" },
                    { 13, true, null, null, 13, "N" },
                    { 14, true, null, null, 14, "N" },
                    { 15, true, null, null, 15, "N" },
                    { 16, true, null, null, 16, "N" },
                    { 17, true, null, null, 1, "S" },
                    { 18, true, null, null, 2, "S" },
                    { 19, true, null, null, 3, "S" },
                    { 20, true, null, null, 4, "S" },
                    { 21, true, null, null, 5, "S" },
                    { 22, true, null, null, 6, "S" },
                    { 23, true, null, null, 7, "S" },
                    { 24, true, null, null, 8, "S" },
                    { 25, true, null, null, 9, "S" },
                    { 26, true, null, null, 10, "S" },
                    { 27, true, null, null, 11, "S" },
                    { 28, true, null, null, 12, "S" },
                    { 29, true, null, null, 13, "S" },
                    { 30, true, null, null, 14, "S" },
                    { 31, true, null, null, 15, "S" },
                    { 32, true, null, null, 16, "S" }
                });

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LeasedBedPriceCents",
                value: 8000);

            migrationBuilder.CreateIndex(
                name: "IX_BedLeasePayment_BedLeaseId",
                table: "BedLeasePayments",
                column: "BedLeaseId");

            migrationBuilder.CreateIndex(
                name: "IX_BedLeasePayment_StripeCheckoutSessionId",
                table: "BedLeasePayments",
                column: "StripeCheckoutSessionId",
                unique: true,
                filter: "[StripeCheckoutSessionId] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_BedLeasePayments_UserId",
                table: "BedLeasePayments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BedLease_BedId",
                table: "BedLeases",
                column: "BedId");

            migrationBuilder.CreateIndex(
                name: "IX_BedLease_UserId",
                table: "BedLeases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BedRequest_Status_CreatedAtUtc",
                table: "BedRequests",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BedRequest_UserId",
                table: "BedRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bed_Section_Number",
                table: "Beds",
                columns: new[] { "Section", "Number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BedLeasePayments");

            migrationBuilder.DropTable(
                name: "BedRequests");

            migrationBuilder.DropTable(
                name: "BedLeases");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropColumn(
                name: "LeasedBedPriceCents",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "MembershipPayments");
        }
    }
}
