using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jawlah.Infrastructure.Data.Migrations
{
    public partial class RemoveUnusedTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "FileMetadata");

            migrationBuilder.DropTable(
                name: "SyncLogs");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FileMetadata",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsOrphaned = table.Column<bool>(type: "bit", nullable: false),
                    MarkedForDeletionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "SyncLogs",
                columns: table => new
                {
                    SyncLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    AppVersion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ConflictDetails = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConflictResolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DeviceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HadConflict = table.Column<bool>(type: "bit", nullable: false),
                    SyncTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLogs", x => x.SyncLogId);
                    table.ForeignKey(
                        name: "FK_SyncLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_User_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType",
                table: "AuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

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

            migrationBuilder.CreateIndex(
                name: "IX_SyncLog_Entity",
                table: "SyncLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLog_User_SyncTime",
                table: "SyncLogs",
                columns: new[] { "UserId", "SyncTime" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncLogs_HadConflict",
                table: "SyncLogs",
                column: "HadConflict");
        }
    }
}
