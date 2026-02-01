using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    public partial class AddPhotosTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    PhotoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IssueId = table.Column<int>(type: "int", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.PhotoId);
                    table.ForeignKey(
                        name: "FK_Photos_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "IssueId");
                    table.ForeignKey(
                        name: "FK_Photos_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "TaskId");
                    table.ForeignKey(
                        name: "FK_Photos_Users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_EntityType_EntityId",
                table: "Photos",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_IssueId",
                table: "Photos",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_TaskId",
                table: "Photos",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UploadedAt",
                table: "Photos",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UploadedByUserId",
                table: "Photos",
                column: "UploadedByUserId");

            // Migrate existing Issue PhotoUrl data to Photos table
            migrationBuilder.Sql(@"
                -- Migrate Issue Photos from PhotoUrl field
                INSERT INTO Photos (PhotoUrl, EntityType, EntityId, OrderIndex, UploadedAt, IssueId)
                SELECT
                    LTRIM(RTRIM(value)) AS PhotoUrl,
                    'Issue' AS EntityType,
                    i.IssueId AS EntityId,
                    ROW_NUMBER() OVER (PARTITION BY i.IssueId ORDER BY (SELECT NULL)) - 1 AS OrderIndex,
                    i.ReportedAt AS UploadedAt,
                    i.IssueId
                FROM Issues i
                CROSS APPLY STRING_SPLIT(i.PhotoUrl, ';')
                WHERE i.PhotoUrl IS NOT NULL AND i.PhotoUrl != '' AND LTRIM(RTRIM(value)) != '';

                -- Migrate Issue Photos from AdditionalPhotosJson field (if any)
                INSERT INTO Photos (PhotoUrl, EntityType, EntityId, OrderIndex, UploadedAt, IssueId)
                SELECT
                    LTRIM(RTRIM(value)) AS PhotoUrl,
                    'Issue' AS EntityType,
                    i.IssueId AS EntityId,
                    (SELECT COUNT(*) FROM Photos WHERE EntityType = 'Issue' AND EntityId = i.IssueId) + ROW_NUMBER() OVER (PARTITION BY i.IssueId ORDER BY (SELECT NULL)) - 1 AS OrderIndex,
                    i.ReportedAt AS UploadedAt,
                    i.IssueId
                FROM Issues i
                CROSS APPLY STRING_SPLIT(i.AdditionalPhotosJson, ';')
                WHERE i.AdditionalPhotosJson IS NOT NULL AND i.AdditionalPhotosJson != '' AND LTRIM(RTRIM(value)) != '';

                -- Migrate Task Photos from PhotoUrl field
                INSERT INTO Photos (PhotoUrl, EntityType, EntityId, OrderIndex, UploadedAt, TaskId)
                SELECT
                    LTRIM(RTRIM(value)) AS PhotoUrl,
                    'Task' AS EntityType,
                    t.TaskId AS EntityId,
                    ROW_NUMBER() OVER (PARTITION BY t.TaskId ORDER BY (SELECT NULL)) - 1 AS OrderIndex,
                    COALESCE(t.CompletedAt, t.CreatedAt) AS UploadedAt,
                    t.TaskId
                FROM Tasks t
                CROSS APPLY STRING_SPLIT(t.PhotoUrl, ';')
                WHERE t.PhotoUrl IS NOT NULL AND t.PhotoUrl != '' AND LTRIM(RTRIM(value)) != '';
            ");

            // Note: We're keeping PhotoUrl and AdditionalPhotosJson columns for now
            // They will be removed in a future migration after verification
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Photos");
        }
    }
}
