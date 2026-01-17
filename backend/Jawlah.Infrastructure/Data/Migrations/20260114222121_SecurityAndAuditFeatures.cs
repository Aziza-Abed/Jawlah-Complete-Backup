using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jawlah.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SecurityAndAuditFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockoutEndTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Attendances",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByUserId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsManual",
                table: "Attendances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManualReason",
                table: "Attendances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_ApprovedByUserId",
                table: "Attendances",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Users_ApprovedByUserId",
                table: "Attendances",
                column: "ApprovedByUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Users_ApprovedByUserId",
                table: "Attendances");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_ApprovedByUserId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LockoutEndTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "IsManual",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "ManualReason",
                table: "Attendances");
        }
    }
}
