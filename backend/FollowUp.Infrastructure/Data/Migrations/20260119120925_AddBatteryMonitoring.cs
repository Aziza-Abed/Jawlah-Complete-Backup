using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBatteryMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLowBattery",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LastBatteryLevel",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBatteryReportTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatteryLevel",
                table: "LocationHistories",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLowBattery",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastBatteryLevel",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastBatteryReportTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "BatteryLevel",
                table: "LocationHistories");
        }
    }
}
