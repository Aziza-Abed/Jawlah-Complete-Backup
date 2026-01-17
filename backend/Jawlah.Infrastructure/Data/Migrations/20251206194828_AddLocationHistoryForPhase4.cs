using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jawlah.Infrastructure.Data.Migrations
{
    public partial class AddLocationHistoryForPhase4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LocationHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: true),
                    Accuracy = table.Column<double>(type: "float", nullable: true),
                    Heading = table.Column<double>(type: "float", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsSync = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationHistories_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LocationHistories_UserId_Timestamp",
                table: "LocationHistories",
                columns: new[] { "UserId", "Timestamp" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LocationHistories");
        }
    }
}
