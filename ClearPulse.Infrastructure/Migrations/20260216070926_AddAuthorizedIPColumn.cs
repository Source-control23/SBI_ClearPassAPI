using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorizedIPColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorizedIP",
                table: "SystemUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorizedIP",
                table: "SystemUsers");
        }
    }
}
