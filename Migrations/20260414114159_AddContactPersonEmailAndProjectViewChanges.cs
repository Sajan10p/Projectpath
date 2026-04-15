using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Projectpath.Migrations
{
    /// <inheritdoc />
    public partial class AddContactPersonEmailAndProjectViewChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactPersonEmail",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactPersonEmail",
                table: "Projects");
        }
    }
}
