using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MultiMunicipalitySupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create Municipalities table first
            migrationBuilder.CreateTable(
                name: "Municipalities",
                columns: table => new
                {
                    MunicipalityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEnglish = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MinLatitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: false),
                    MaxLatitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: false),
                    MinLongitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: false),
                    MaxLongitude = table.Column<double>(type: "float(18)", precision: 18, scale: 15, nullable: false),
                    DefaultStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DefaultEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DefaultGraceMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 15),
                    MaxAcceptableAccuracyMeters = table.Column<double>(type: "float", nullable: false, defaultValue: 150.0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LicenseExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Municipalities", x => x.MunicipalityId);
                });

            // Step 2: Create indexes on Municipalities table
            migrationBuilder.CreateIndex(
                name: "IX_Municipality_Code_Unique",
                table: "Municipalities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Municipality_IsActive",
                table: "Municipalities",
                column: "IsActive");

            // Step 3: Insert default Al-Bireh municipality (ID will be 1)
            // Al-Bireh bounding box from GeofencingConstants
            migrationBuilder.Sql(@"
                INSERT INTO Municipalities (Code, Name, NameEnglish, Country, Region,
                    MinLatitude, MaxLatitude, MinLongitude, MaxLongitude,
                    DefaultStartTime, DefaultEndTime, DefaultGraceMinutes, MaxAcceptableAccuracyMeters,
                    IsActive, CreatedAt)
                VALUES ('ALBIREH', N'بلدية البيرة', 'Al-Bireh Municipality', 'Palestine', N'رام الله والبيرة',
                    31.87, 31.95, 35.18, 35.27,
                    '08:00:00', '16:00:00', 15, 150.0,
                    1, GETUTCDATE())
            ");

            // Step 4: Add MunicipalityId columns to existing tables with default value 1 (Al-Bireh)
            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Zones",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Issues",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MunicipalityId",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 1);

            // Step 5: Create indexes on the new MunicipalityId columns
            migrationBuilder.CreateIndex(
                name: "IX_Zones_MunicipalityId",
                table: "Zones",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_MunicipalityId",
                table: "Users",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_MunicipalityId",
                table: "Tasks",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_MunicipalityId",
                table: "Notifications",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_MunicipalityId",
                table: "Issues",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_MunicipalityId",
                table: "Attendances",
                column: "MunicipalityId");

            // Step 6: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Municipalities_MunicipalityId",
                table: "Attendances",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Municipalities_MunicipalityId",
                table: "Issues",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Municipalities_MunicipalityId",
                table: "Notifications",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Municipalities_MunicipalityId",
                table: "Tasks",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Municipalities_MunicipalityId",
                table: "Users",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Zones_Municipalities_MunicipalityId",
                table: "Zones",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "MunicipalityId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Municipalities_MunicipalityId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Municipalities_MunicipalityId",
                table: "Issues");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Municipalities_MunicipalityId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Municipalities_MunicipalityId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Municipalities_MunicipalityId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_Zones_Municipalities_MunicipalityId",
                table: "Zones");

            migrationBuilder.DropTable(
                name: "Municipalities");

            migrationBuilder.DropIndex(
                name: "IX_Zones_MunicipalityId",
                table: "Zones");

            migrationBuilder.DropIndex(
                name: "IX_Users_MunicipalityId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_MunicipalityId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_MunicipalityId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Issues_MunicipalityId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_MunicipalityId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Zones");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Attendances");
        }
    }
}
