using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Municipalities_MunicipalityId",
                table: "TaskTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Users_DefaultAssignedToUserId",
                table: "TaskTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Zones_ZoneId",
                table: "TaskTemplates");

            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplates_MunicipalityId",
                table: "TaskTemplates",
                newName: "IX_TaskTemplate_MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplate_IsActive",
                table: "TaskTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TaskTemplate_Municipality_Active",
                table: "TaskTemplates",
                columns: new[] { "MunicipalityId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Municipalities_MunicipalityId",
                table: "TaskTemplates",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Users_DefaultAssignedToUserId",
                table: "TaskTemplates",
                column: "DefaultAssignedToUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Zones_ZoneId",
                table: "TaskTemplates",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "ZoneId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Municipalities_MunicipalityId",
                table: "TaskTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Users_DefaultAssignedToUserId",
                table: "TaskTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskTemplates_Zones_ZoneId",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplate_IsActive",
                table: "TaskTemplates");

            migrationBuilder.DropIndex(
                name: "IX_TaskTemplate_Municipality_Active",
                table: "TaskTemplates");

            migrationBuilder.RenameIndex(
                name: "IX_TaskTemplate_MunicipalityId",
                table: "TaskTemplates",
                newName: "IX_TaskTemplates_MunicipalityId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Municipalities_MunicipalityId",
                table: "TaskTemplates",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Users_DefaultAssignedToUserId",
                table: "TaskTemplates",
                column: "DefaultAssignedToUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskTemplates_Zones_ZoneId",
                table: "TaskTemplates",
                column: "ZoneId",
                principalTable: "Zones",
                principalColumn: "ZoneId");
        }
    }
}
