using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhanceTaskTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultAssignedToUserId",
                table: "TaskTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultTeamId",
                table: "TaskTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "TaskTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsTeamTask",
                table: "TaskTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LocationDescription",
                table: "TaskTemplates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TaskTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPhotoProof",
                table: "TaskTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TaskType",
                table: "TaskTemplates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplates_DefaultAssignedToUserId",
                table: "TaskTemplates",
                column: "DefaultAssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Users_DefaultAssignedToUserId",
                table: "TaskTemplates",
                column: "DefaultAssignedToUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Users_DefaultAssignedToUserId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplates_DefaultAssignedToUserId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultAssignedToUserId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultTeamId",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "IsTeamTask",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "LocationDescription",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "RequiresPhotoProof",
                table: "TaskTemplates");

            migrationBuilder.DropColumn(
                name: "TaskType",
                table: "TaskTemplates");
        }
    }
}
