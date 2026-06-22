using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAndLabelBeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bed_Section_Number",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "Number",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "Section",
                table: "Beds");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "Beds",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Beds",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N1" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N2" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N3" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N4" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N5" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N6" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N7" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N8" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N9" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N10" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N11" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N12" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N13" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N14" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N15" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "N16" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S1" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S2" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S3" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S4" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S5" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S6" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S7" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S8" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S9" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S10" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S11" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S12" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S13" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S14" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S15" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "IsDeleted", "Label" },
                values: new object[] { false, "S16" });

            migrationBuilder.CreateIndex(
                name: "IX_Bed_Label",
                table: "Beds",
                column: "Label",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bed_Label",
                table: "Beds");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Beds");

            migrationBuilder.AlterColumn<string>(
                name: "Label",
                table: "Beds",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Beds",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Section",
                table: "Beds",
                type: "nvarchar(1)",
                maxLength: 1,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 1, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 2, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 3, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 4, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 5, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 6, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 7, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 8, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 9, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 10, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 11, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 12, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 13, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 14, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 15, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 16, "N" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 1, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 2, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 3, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 4, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 5, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 6, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 7, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 8, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 9, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 10, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 11, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 12, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 13, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 14, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 31,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 15, "S" });

            migrationBuilder.UpdateData(
                table: "Beds",
                keyColumn: "Id",
                keyValue: 32,
                columns: new[] { "Label", "Number", "Section" },
                values: new object[] { null, 16, "S" });

            migrationBuilder.CreateIndex(
                name: "IX_Bed_Section_Number",
                table: "Beds",
                columns: new[] { "Section", "Number" },
                unique: true);
        }
    }
}
