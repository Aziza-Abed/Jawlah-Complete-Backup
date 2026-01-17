using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jawlah.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceForwardingWithPdfDownload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueForwardings");

            migrationBuilder.DropTable(
                name: "ExternalDepartments");

            migrationBuilder.AddColumn<string>(
                name: "ForwardingNotes",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PdfDownloadedAt",
                table: "Issues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PdfDownloadedByUserId",
                table: "Issues",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForwardingNotes",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "PdfDownloadedAt",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "PdfDownloadedByUserId",
                table: "Issues");

            migrationBuilder.CreateTable(
                name: "ExternalDepartments",
                columns: table => new
                {
                    DepartmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameArabic = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDepartments", x => x.DepartmentId);
                });

            migrationBuilder.CreateTable(
                name: "IssueForwardings",
                columns: table => new
                {
                    ForwardingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    ForwardedByUserId = table.Column<int>(type: "int", nullable: false),
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    ForwardedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueForwardings", x => x.ForwardingId);
                    table.ForeignKey(
                        name: "FK_IssueForwardings_ExternalDepartments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "ExternalDepartments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueForwardings_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueForwardings_Users_ForwardedByUserId",
                        column: x => x.ForwardedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDepartments_IsActive",
                table: "ExternalDepartments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDepartments_Name",
                table: "ExternalDepartments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueForwardings_DepartmentId",
                table: "IssueForwardings",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueForwardings_ForwardedAt",
                table: "IssueForwardings",
                column: "ForwardedAt");

            migrationBuilder.CreateIndex(
                name: "IX_IssueForwardings_ForwardedByUserId",
                table: "IssueForwardings",
                column: "ForwardedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueForwardings_IssueId",
                table: "IssueForwardings",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueForwardings_Status",
                table: "IssueForwardings",
                column: "Status");
        }
    }
}
