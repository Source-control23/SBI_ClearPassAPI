using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QuarantineDetail",
                table: "QuarantineDetail");

            migrationBuilder.RenameTable(
                name: "QuarantineDetail",
                newName: "QuarantineDetails");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuarantineDetails",
                table: "QuarantineDetails",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_QuarantineDetails",
                table: "QuarantineDetails");

            migrationBuilder.RenameTable(
                name: "QuarantineDetails",
                newName: "QuarantineDetail");

            migrationBuilder.AddPrimaryKey(
                name: "PK_QuarantineDetail",
                table: "QuarantineDetail",
                column: "Id");
        }
    }
}
