using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstagramPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InstagramImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Bytes = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstagramImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstagramImages_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "InstagramPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageId = table.Column<int>(type: "int", nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Permalink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstagramPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstagramPosts_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InstagramPosts_InstagramImages_ImageId",
                        column: x => x.ImageId,
                        principalTable: "InstagramImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstagramImages_UploadedByUserId",
                table: "InstagramImages",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramPost_DisplayOrder",
                table: "InstagramPosts",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramPosts_CreatedByUserId",
                table: "InstagramPosts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramPosts_ImageId",
                table: "InstagramPosts",
                column: "ImageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstagramPosts");

            migrationBuilder.DropTable(
                name: "InstagramImages");
        }
    }
}
