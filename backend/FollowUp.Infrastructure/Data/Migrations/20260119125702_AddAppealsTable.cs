using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppealsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatteryLevel",
                table: "LocationHistories");

            migrationBuilder.CreateTable(
                name: "Appeals",
                columns: table => new
                {
                    AppealId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MunicipalityId = table.Column<int>(type: "int", nullable: false),
                    AppealType = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    WorkerExplanation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    WorkerLatitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: true),
                    WorkerLongitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: true),
                    ExpectedLatitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: true),
                    ExpectedLongitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: true),
                    DistanceMeters = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EvidencePhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalRejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSynced = table.Column<bool>(type: "bit", nullable: false),
                    SyncVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appeals", x => x.AppealId);
                    table.ForeignKey(
                        name: "FK_Appeals_Municipalities_MunicipalityId",
                        column: x => x.MunicipalityId,
                        principalTable: "Municipalities",
                        principalColumn: "MunicipalityId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appeals_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appeals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appeal_Entity",
                table: "Appeals",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appeal_Status",
                table: "Appeals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Appeal_SubmittedAt",
                table: "Appeals",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Appeal_User_Status",
                table: "Appeals",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_MunicipalityId",
                table: "Appeals",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Appeals_ReviewedByUserId",
                table: "Appeals",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appeals");

            migrationBuilder.AddColumn<int>(
                name: "BatteryLevel",
                table: "LocationHistories",
                type: "int",
                nullable: true);
        }
    }
}
