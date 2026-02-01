using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EvaluationReadyFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsentVersion",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ExpectedEndTime",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(16, 0, 0)); // 16:00 default

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ExpectedStartTime",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(8, 0, 0)); // 08:00 default

            migrationBuilder.AddColumn<int>(
                name: "GraceMinutes",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 15); // 15 minutes grace period

            migrationBuilder.AddColumn<DateTime>(
                name: "PrivacyConsentedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletionDistanceMeters",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDistanceWarning",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxDistanceMeters",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 100); // 100 meters default radius

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Attendances",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "AutoApproved");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceType",
                table: "Attendances",
                type: "nvarchar(50)",
                nullable: false,
                defaultValue: "OnTime");

            migrationBuilder.AddColumn<int>(
                name: "EarlyLeaveMinutes",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LateMinutes",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OvertimeMinutes",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsentVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExpectedEndTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExpectedStartTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "GraceMinutes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrivacyConsentedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompletionDistanceMeters",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsDistanceWarning",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "MaxDistanceMeters",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "AttendanceType",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "EarlyLeaveMinutes",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "LateMinutes",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "OvertimeMinutes",
                table: "Attendances");
        }
    }
}
