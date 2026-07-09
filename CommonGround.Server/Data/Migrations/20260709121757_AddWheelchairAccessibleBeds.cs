using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWheelchairAccessibleBeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsWheelchairAccessible",
                table: "Beds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresWheelchairAccessible",
                table: "BedRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 1,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 2,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 3,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 4,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 5,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 6,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 7,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 8,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 9,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 10,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 11,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 12,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 13,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 14,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 15,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 16,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 17,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 18,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 19,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 20,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 21,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 22,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 23,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 24,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 25,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 26,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 27,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 28,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 29,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 30,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 31,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 32,
                column: "IsWheelchairAccessible",
                value: false);

            migrationBuilder.InsertData(
                table: "Beds",
                columns: new[] { "Id", "IsActive", "IsDeleted", "IsWheelchairAccessible", "Label", "Notes" },
                values: new object[,]
                {
                    { 33, true, false, true, "W1", null },
                    { 34, true, false, true, "W2", null },
                    { 35, true, false, true, "W3", null },
                    { 36, true, false, true, "W4", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DropColumn(
                name: "IsWheelchairAccessible",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "RequiresWheelchairAccessible",
                table: "BedRequests");
        }
    }
}
