using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jawlah.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePinConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // add unique constraint on PIN column (null values allowed, but non-null must be unique)
            migrationBuilder.CreateIndex(
                name: "IX_Users_Pin_Unique",
                table: "Users",
                column: "Pin",
                unique: true,
                filter: "[Pin] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Pin_Unique",
                table: "Users");
        }
    }
}
