using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FollowUp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePinSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop all indexes on Pin column manually (handles both old and new index names)
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Pin' AND object_id = OBJECT_ID('Users'))
                    DROP INDEX [IX_Users_Pin] ON [Users];

                IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Pin_Unique' AND object_id = OBJECT_ID('Users'))
                    DROP INDEX [IX_Users_Pin_Unique] ON [Users];
            ");

            migrationBuilder.DropColumn(
                name: "Pin",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Pin",
                table: "Users",
                type: "nvarchar(4)",
                maxLength: 4,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Pin",
                table: "Users",
                column: "Pin",
                unique: true,
                filter: "[Pin] IS NOT NULL");
        }
    }
}
