using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jawlah.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tasks",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Issues",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FileMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MarkedForDeletionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsOrphaned = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMetadata_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_EntityId",
                table: "FileMetadata",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_EntityType",
                table: "FileMetadata",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_EntityType_EntityId_IsDeleted",
                table: "FileMetadata",
                columns: new[] { "EntityType", "EntityId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_IsDeleted",
                table: "FileMetadata",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_IsOrphaned",
                table: "FileMetadata",
                column: "IsOrphaned");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_MarkedForDeletionAt",
                table: "FileMetadata",
                column: "MarkedForDeletionAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_UploadedAt",
                table: "FileMetadata",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_UploadedByUserId",
                table: "FileMetadata",
                column: "UploadedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileMetadata");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Issues");
        }
    }
}
