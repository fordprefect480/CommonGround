using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceInstagramSchemaWithEmbeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InstagramPosts_InstagramImages_ImageId",
                table: "InstagramPosts");

            migrationBuilder.DropTable(
                name: "InstagramImages");

            migrationBuilder.DropIndex(
                name: "IX_InstagramPosts_ImageId",
                table: "InstagramPosts");

            migrationBuilder.DropColumn(
                name: "Caption",
                table: "InstagramPosts");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "InstagramPosts");

            migrationBuilder.DropColumn(
                name: "Permalink",
                table: "InstagramPosts");

            migrationBuilder.AddColumn<string>(
                name: "EmbedHtml",
                table: "InstagramPosts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbedHtml",
                table: "InstagramPosts");

            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "InstagramPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ImageId",
                table: "InstagramPosts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Permalink",
                table: "InstagramPosts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstagramImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Bytes = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "IX_InstagramPosts_ImageId",
                table: "InstagramPosts",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_InstagramImages_UploadedByUserId",
                table: "InstagramImages",
                column: "UploadedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InstagramPosts_InstagramImages_ImageId",
                table: "InstagramPosts",
                column: "ImageId",
                principalTable: "InstagramImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
