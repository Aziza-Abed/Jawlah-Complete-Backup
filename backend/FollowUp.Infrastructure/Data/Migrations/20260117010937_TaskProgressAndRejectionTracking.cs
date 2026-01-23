using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class TaskProgressAndRejectionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastWarningAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastWarningReason",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WarningCount",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExtendedByUserId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExtendedDeadline",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoRejected",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ProgressNotes",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercentage",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectedByUserId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectionDistanceMeters",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RejectionLatitude",
                table: "Tasks",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RejectionLongitude",
                table: "Tasks",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastWarningAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastWarningReason",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "WarningCount",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ExtendedByUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ExtendedDeadline",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsAutoRejected",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ProgressNotes",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ProgressPercentage",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RejectionDistanceMeters",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RejectionLatitude",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RejectionLongitude",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Tasks");
        }
    }
}
