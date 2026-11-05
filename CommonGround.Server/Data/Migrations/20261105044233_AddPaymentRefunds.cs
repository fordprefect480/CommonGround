using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RefundedAmountCents",
                table: "MembershipPayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAtUtc",
                table: "MembershipPayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundedAmountCents",
                table: "BedLeasePayments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAtUtc",
                table: "BedLeasePayments",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundedAmountCents",
                table: "MembershipPayments");

            migrationBuilder.DropColumn(
                name: "RefundedAtUtc",
                table: "MembershipPayments");

            migrationBuilder.DropColumn(
                name: "RefundedAmountCents",
                table: "BedLeasePayments");

            migrationBuilder.DropColumn(
                name: "RefundedAtUtc",
                table: "BedLeasePayments");
        }
    }
}
