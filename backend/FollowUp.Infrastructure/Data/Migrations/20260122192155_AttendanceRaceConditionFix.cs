using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceRaceConditionFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingJwtToken",
                table: "TwoFactorCodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionToken",
                table: "TwoFactorCodes",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckInDate",
                table: "Attendances",
                type: "datetime2",
                nullable: false,
                computedColumnSql: "CAST([CheckInEventTime] AS DATE)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_UniqueActiveCheckIn",
                table: "Attendances",
                columns: new[] { "UserId", "CheckInDate" },
                unique: true,
                filter: "[Status] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Attendance_UniqueActiveCheckIn",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "CheckInDate",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "PendingJwtToken",
                table: "TwoFactorCodes");

            migrationBuilder.DropColumn(
                name: "SessionToken",
                table: "TwoFactorCodes");
        }
    }
}
