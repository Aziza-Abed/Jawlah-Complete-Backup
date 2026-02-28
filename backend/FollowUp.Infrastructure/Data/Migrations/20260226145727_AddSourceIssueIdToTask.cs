using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceIssueIdToTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceIssueId",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EventTime",
                table: "Photos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Photos",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Photos",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SyncTime",
                table: "Photos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EntityId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceIssueId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "EventTime",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "SyncTime",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AuditLogs");
        }
    }
}
