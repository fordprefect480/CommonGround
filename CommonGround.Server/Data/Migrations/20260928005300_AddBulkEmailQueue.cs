using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkEmailQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicBaseUrl",
                table: "SentEmails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QueuedCount",
                table: "SentEmails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAtUtc",
                table: "SentEmailRecipients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SentEmailRecipient_Status_SentAtUtc",
                table: "SentEmailRecipients",
                columns: new[] { "Status", "SentAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SentEmailRecipient_Status_SentAtUtc",
                table: "SentEmailRecipients");

            migrationBuilder.DropColumn(
                name: "PublicBaseUrl",
                table: "SentEmails");

            migrationBuilder.DropColumn(
                name: "QueuedCount",
                table: "SentEmails");

            migrationBuilder.DropColumn(
                name: "SentAtUtc",
                table: "SentEmailRecipients");
        }
    }
}
