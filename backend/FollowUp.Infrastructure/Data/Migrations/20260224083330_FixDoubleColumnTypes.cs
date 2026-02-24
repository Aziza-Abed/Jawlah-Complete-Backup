using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixDoubleColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "CenterLongitude",
                table: "Zones",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "CenterLatitude",
                table: "Zones",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Tasks",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Tasks",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "MinLongitude",
                table: "Municipalities",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "MinLatitude",
                table: "Municipalities",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "MaxLongitude",
                table: "Municipalities",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "MaxLatitude",
                table: "Municipalities",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "LocationHistories",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "LocationHistories",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Issues",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Issues",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "CheckOutLongitude",
                table: "Attendances",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CheckOutLatitude",
                table: "Attendances",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CheckOutAccuracyMeters",
                table: "Attendances",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(10)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CheckInLongitude",
                table: "Attendances",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "CheckInLatitude",
                table: "Attendances",
                type: "float",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15);

            migrationBuilder.AlterColumn<double>(
                name: "CheckInAccuracyMeters",
                table: "Attendances",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(10)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "WorkerLongitude",
                table: "Appeals",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "WorkerLatitude",
                table: "Appeals",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "ExpectedLongitude",
                table: "Appeals",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "ExpectedLatitude",
                table: "Appeals",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float(18)",
                oldPrecision: 18,
                oldScale: 15,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "CenterLongitude",
                table: "Zones",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "CenterLatitude",
                table: "Zones",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Tasks",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Tasks",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "MinLongitude",
                table: "Municipalities",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "MinLatitude",
                table: "Municipalities",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "MaxLongitude",
                table: "Municipalities",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "MaxLatitude",
                table: "Municipalities",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "LocationHistories",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "LocationHistories",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Issues",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Issues",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "CheckOutLongitude",
                table: "Attendances",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CheckOutLatitude",
                table: "Attendances",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CheckOutAccuracyMeters",
                table: "Attendances",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "CheckInLongitude",
                table: "Attendances",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "CheckInLatitude",
                table: "Attendances",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: false,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "CheckInAccuracyMeters",
                table: "Attendances",
                type: "float(10)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "WorkerLongitude",
                table: "Appeals",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "WorkerLatitude",
                table: "Appeals",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "ExpectedLongitude",
                table: "Appeals",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "ExpectedLatitude",
                table: "Appeals",
                type: "float(18)",
                precision: 18,
                scale: 15,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);
        }
    }
}
