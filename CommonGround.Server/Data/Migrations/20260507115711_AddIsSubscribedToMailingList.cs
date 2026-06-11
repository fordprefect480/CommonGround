using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSubscribedToMailingList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubscribedToMailingList",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubscribedToMailingList",
                table: "AspNetUsers");
        }
    }
}
