using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HealthStatus",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NodeRole",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DomainName",
                table: "AdIntegrations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HealthStatus",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "NodeRole",
                table: "ClusterNodes");

            migrationBuilder.DropColumn(
                name: "DomainName",
                table: "AdIntegrations");
        }
    }
}
