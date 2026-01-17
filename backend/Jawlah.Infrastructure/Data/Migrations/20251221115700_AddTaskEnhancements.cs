using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jawlah.Infrastructure.Data.Migrations
{
    public partial class AddTaskEnhancements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TaskType",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPhotoProof",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Tasks",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RequiresPhotoProof",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Tasks");
        }
    }
}
