using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSystemUserIdToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the Primary Key constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_SystemUsers",
                table: "SystemUsers");

            // Step 2: Change the column type from bigint to int
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "SystemUsers",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            // Step 3: Recreate the Primary Key constraint
            migrationBuilder.AddPrimaryKey(
                name: "PK_SystemUsers",
                table: "SystemUsers",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the Primary Key constraint
            migrationBuilder.DropPrimaryKey(
                name: "PK_SystemUsers",
                table: "SystemUsers");

            // Step 2: Change the column type back from int to bigint
            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "SystemUsers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            // Step 3: Recreate the Primary Key constraint
            migrationBuilder.AddPrimaryKey(
                name: "PK_SystemUsers",
                table: "SystemUsers",
                column: "Id");
        }
    }
}
