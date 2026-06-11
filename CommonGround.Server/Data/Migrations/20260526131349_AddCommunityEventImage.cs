using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityEventImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeaturedImageId",
                table: "CommunityEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommunityEvents_FeaturedImageId",
                table: "CommunityEvents",
                column: "FeaturedImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_CommunityEvents_BlogImages_FeaturedImageId",
                table: "CommunityEvents",
                column: "FeaturedImageId",
                principalTable: "BlogImages",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommunityEvents_BlogImages_FeaturedImageId",
                table: "CommunityEvents");

            migrationBuilder.DropIndex(
                name: "IX_CommunityEvents_FeaturedImageId",
                table: "CommunityEvents");

            migrationBuilder.DropColumn(
                name: "FeaturedImageId",
                table: "CommunityEvents");
        }
    }
}
