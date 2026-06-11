using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSentEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SentEmails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TextBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SenderEmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    SentCount = table.Column<int>(type: "int", nullable: false),
                    FailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentEmails_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SentEmailRecipients",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SentEmailId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SentEmailRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SentEmailRecipients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SentEmailRecipients_SentEmails_SentEmailId",
                        column: x => x.SentEmailId,
                        principalTable: "SentEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SentEmailRecipient_SentEmailId",
                table: "SentEmailRecipients",
                column: "SentEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_SentEmailRecipients_UserId",
                table: "SentEmailRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SentEmail_SentAt",
                table: "SentEmails",
                column: "SentAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_SentEmails_SenderUserId",
                table: "SentEmails",
                column: "SenderUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SentEmailRecipients");

            migrationBuilder.DropTable(
                name: "SentEmails");
        }
    }
}
