using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CommonGround.Server.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Activities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ActivityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorEmailSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TargetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Activities_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_Actor_OccurredAt",
                table: "Activities",
                columns: new[] { "ActorUserId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Activity_OccurredAt",
                table: "Activities",
                column: "OccurredAt",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Activity_Type_OccurredAt",
                table: "Activities",
                columns: new[] { "ActivityType", "OccurredAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Activities");
        }
    }
}
