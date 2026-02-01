using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncSchemaCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Role",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Role_Status",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Status",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_AssignedToUserId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_Status",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_SyncTime",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Issues_ReportedByUserId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Issues_Status",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_CheckInEventTime",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_UserId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "AdditionalPhotosJson",
                table: "Issues");

            migrationBuilder.RenameIndex(
                name: "IX_LocationHistories_UserId_Timestamp",
                table: "LocationHistories",
                newName: "IX_LocationHistory_User_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_Attendances_ZoneId",
                table: "Attendances",
                newName: "IX_Attendance_ZoneId");

            migrationBuilder.RenameIndex(
                name: "IX_Attendances_Status",
                table: "Attendances",
                newName: "IX_Attendance_Status");

            migrationBuilder.AlterColumn<string>(
                name: "FcmToken",
                table: "Users",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "LocationHistories",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "LocationHistories",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.CreateIndex(
                name: "IX_UserZones_AssignedByUserId",
                table: "UserZones",
                column: "AssignedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserZones_Users_AssignedByUserId",
                table: "UserZones",
                column: "AssignedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserZones_Users_AssignedByUserId",
                table: "UserZones");

            migrationBuilder.DropIndex(
                name: "IX_UserZones_AssignedByUserId",
                table: "UserZones");

            migrationBuilder.RenameIndex(
                name: "IX_LocationHistory_User_Timestamp",
                table: "LocationHistories",
                newName: "IX_LocationHistories_UserId_Timestamp");

            migrationBuilder.RenameIndex(
                name: "IX_Attendance_ZoneId",
                table: "Attendances",
                newName: "IX_Attendances_ZoneId");

            migrationBuilder.RenameIndex(
                name: "IX_Attendance_Status",
                table: "Attendances",
                newName: "IX_Attendances_Status");

            migrationBuilder.AlterColumn<string>(
                name: "FcmToken",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcmToken",
                table: "Notifications",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "LocationHistories",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "LocationHistories",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalPhotosJson",
                table: "Issues",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role_Status",
                table: "Users",
                columns: new[] { "Role", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Status",
                table: "Users",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedToUserId",
                table: "Tasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                table: "Tasks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_SyncTime",
                table: "Tasks",
                column: "SyncTime");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReportedByUserId",
                table: "Issues",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Status",
                table: "Issues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_CheckInEventTime",
                table: "Attendances",
                column: "CheckInEventTime");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_UserId",
                table: "Attendances",
                column: "UserId");
        }
    }
}
