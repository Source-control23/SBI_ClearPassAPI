using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearPulse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "SystemUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "SystemUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "SystemUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "SystemUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLogin",
                table: "SystemUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "SystemUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "SystemUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "SystemUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "NasDevices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RequestCount",
                table: "NasDevices",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NasIp",
                table: "NasDevices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSeen",
                table: "NasDevices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "NasDevices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "NasDevices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "NasDevices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                table: "NasDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchName",
                table: "NasDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceModel",
                table: "NasDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmwareVersion",
                table: "NasDevices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "NasDevices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "LogEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "LogEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "LogEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "LogEvents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DeviceAuthEvents",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "DeviceAuthEvents",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MacAddress",
                table: "DeviceAuthEvents",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HealthStatus",
                table: "DeviceAuthEvents",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "DeviceAuthEvents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "DeviceAuthEvents",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "DeviceAuthEvents",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AuthType",
                table: "DeviceAuthEvents",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ADAccountDisabled",
                table: "DeviceAuthEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ADCertificateValid",
                table: "DeviceAuthEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ADDomain",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ADUserFound",
                table: "DeviceAuthEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AVInstalled",
                table: "DeviceAuthEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AVProduct",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AVScanned",
                table: "DeviceAuthEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AVUpdated",
                table: "DeviceAuthEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AuthMethod",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BranchCode",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CriticalPatchesMissing",
                table: "DeviceAuthEvents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hostname",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IsolationState",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastPatchDate",
                table: "DeviceAuthEvents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuarantineCategory",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuarantineReason",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WindowsPatchLevel",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClusterNodes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "ClusterNodes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NodeIp",
                table: "ClusterNodes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "ClusterNodes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "ClusterNodes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "ClusterNodes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CpuUsage",
                table: "ClusterNodes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MemoryUsage",
                table: "ClusterNodes",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "NodeName",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReplicationStatus",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AdIntegrations",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "AdIntegrations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "AdIntegrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "AdIntegrations",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "AdIntegrations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ErrorCode",
                table: "AdIntegrations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BranchMaster",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BranchCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CircleCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CircleMaster",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CircleCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CircleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Region = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    State = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircleMaster", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceInventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MacAddress = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Hostname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OSVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CurrentHealthStatus = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LastSeenOnline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAuthSuccess = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAuthFailure = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AVInstalled = table.Column<bool>(type: "bit", nullable: true),
                    AVProduct = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AVLastUpdate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AVLastScan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: true),
                    ComplianceIssues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CircleCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceInventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuarantineDetail",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MacAddress = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    QuarantineType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuarantineReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetailedMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuarantineStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuarantineEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ResolvedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CircleCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EventTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuarantineDetail", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NasDevices_LastSeen",
                table: "NasDevices",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthEvents_AuthType_HealthStatus_EventTime",
                table: "DeviceAuthEvents",
                columns: new[] { "AuthType", "HealthStatus", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuthEvents_Status_EventTime",
                table: "DeviceAuthEvents",
                columns: new[] { "Status", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodes_NodeIp_EventTime",
                table: "ClusterNodes",
                columns: new[] { "NodeIp", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ClusterNodes_Status_EventTime",
                table: "ClusterNodes",
                columns: new[] { "Status", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AdIntegrations_Status_ErrorCode_EventTime",
                table: "AdIntegrations",
                columns: new[] { "Status", "ErrorCode", "EventTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchMaster_BranchCode_TenantId",
                table: "BranchMaster",
                columns: new[] { "BranchCode", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BranchMaster_CircleCode_TenantId",
                table: "BranchMaster",
                columns: new[] { "CircleCode", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_CircleMaster_CircleCode_TenantId",
                table: "CircleMaster",
                columns: new[] { "CircleCode", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventory_CircleCode_CurrentHealthStatus",
                table: "DeviceInventory",
                columns: new[] { "CircleCode", "CurrentHealthStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventory_LastSeenOnline",
                table: "DeviceInventory",
                column: "LastSeenOnline");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceInventory_MacAddress_TenantId",
                table: "DeviceInventory",
                columns: new[] { "MacAddress", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchMaster");

            migrationBuilder.DropTable(
                name: "CircleMaster");

            migrationBuilder.DropTable(
                name: "DeviceInventory");

            migrationBuilder.DropTable(
                name: "QuarantineDetail");

            migrationBuilder.DropIndex(
                name: "IX_NasDevices_LastSeen",
                table: "NasDevices");

            migrationBuilder.DropIndex(
                name: "IX_DeviceAuthEvents_AuthType_HealthStatus_EventTime",
                table: "DeviceAuthEvents");

            migrationBuilder.DropIndex(
                name: "IX_DeviceAuthEvents_Status_EventTime",
                table: "DeviceAuthEvents");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodes_NodeIp_EventTime",
                table: "ClusterNodes");

            migrationBuilder.DropIndex(
                name: "IX_ClusterNodes_Status_EventTime",
                table: "ClusterNodes");

            migrationBuilder.DropIndex(
                name: "IX_AdIntegrations_Status_ErrorCode_EventTime",
                table: "AdIntegrations");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "SystemUsers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "SystemUsers");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "NasDevices");

            migrationBuilder.DropColumn(
                name: "BranchName",
                table: "NasDevices");

            migrationBuilder.DropColumn(
                name: "DeviceModel",
                table: "NasDevices");

            migrationBuilder.DropColumn(
                name: "FirmwareVersion",
                table: "NasDevices");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "NasDevices");

            migrationBuilder.DropColumn(
                name: "ADAccountDisabled",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "ADCertificateValid",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "ADDomain",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "ADUserFound",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "AVInstalled",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "AVProduct",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "AVScanned",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "AVUpdated",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "AuthMethod",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "BranchCode",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "CriticalPatchesMissing",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "Hostname",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "IsolationState",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "LastPatchDate",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "QuarantineCategory",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "QuarantineReason",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "WindowsPatchLevel",
                table: "DeviceAuthEvents");

            migrationBuilder.DropColumn(
                name: "CpuUsage",
                table: "ClusterNodes");

            migrationBuilder.DropColumn(
                name: "MemoryUsage",
                table: "ClusterNodes");

            migrationBuilder.DropColumn(
                name: "NodeName",
                table: "ClusterNodes");

            migrationBuilder.DropColumn(
                name: "ReplicationStatus",
                table: "ClusterNodes");

            migrationBuilder.DropColumn(
                name: "ErrorCode",
                table: "AdIntegrations");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "SystemUsers",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "SystemUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "SystemUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "SystemUsers",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastLogin",
                table: "SystemUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "SystemUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "NasDevices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RequestCount",
                table: "NasDevices",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "NasIp",
                table: "NasDevices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastSeen",
                table: "NasDevices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "NasDevices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "NasDevices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "NasDevices",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "LogEvents",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "LogEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "LogEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "LogEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MacAddress",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(12)",
                oldMaxLength: 12);

            migrationBuilder.AlterColumn<string>(
                name: "HealthStatus",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "DeviceAuthEvents",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "DeviceAuthEvents",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AuthType",
                table: "DeviceAuthEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NodeIp",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "ClusterNodes",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "ClusterNodes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "AdIntegrations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceSystem",
                table: "AdIntegrations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "AdIntegrations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EventTime",
                table: "AdIntegrations",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "CircleCode",
                table: "AdIntegrations",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
