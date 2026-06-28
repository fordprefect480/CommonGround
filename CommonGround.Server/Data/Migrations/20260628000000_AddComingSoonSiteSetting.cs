using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComingSoonSiteSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ComingSoon",
                table: "SiteSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComingSoon",
                table: "SiteSettings");
        }
    }
}
