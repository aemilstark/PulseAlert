using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseAlert.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    IMEI = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.DeviceId);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryPings",
                columns: table => new
                {
                    PingId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: false),
                    AltitudeMeters = table.Column<double>(type: "double precision", nullable: true),
                    BatteryPercentage = table.Column<int>(type: "integer", nullable: false),
                    IsSosActive = table.Column<bool>(type: "boolean", nullable: false),
                    PowerOff = table.Column<bool>(type: "boolean", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryPings", x => x.PingId);
                    table.CheckConstraint("CK_TelemetryPings_BatteryPercentage", "\"BatteryPercentage\" BETWEEN 0 AND 100");
                    table.CheckConstraint("CK_TelemetryPings_Latitude", "\"Latitude\" BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_TelemetryPings_Longitude", "\"Longitude\" BETWEEN -180 AND 180");
                    table.ForeignKey(
                        name: "FK_TelemetryPings_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmergencyIncidents",
                columns: table => new
                {
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerPingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    OpenedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyIncidents", x => x.IncidentId);
                    table.ForeignKey(
                        name: "FK_EmergencyIncidents_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmergencyIncidents_TelemetryPings_TriggerPingId",
                        column: x => x.TriggerPingId,
                        principalTable: "TelemetryPings",
                        principalColumn: "PingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_IMEI",
                table: "Devices",
                column: "IMEI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyIncidents_DeviceId_Unresolved",
                table: "EmergencyIncidents",
                column: "DeviceId",
                unique: true,
                filter: "\"Status\" <> 'Resolved'");

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyIncidents_TriggerPingId",
                table: "EmergencyIncidents",
                column: "TriggerPingId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryPings_DeviceId_RecordedAtUtc",
                table: "TelemetryPings",
                columns: new[] { "DeviceId", "RecordedAtUtc" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmergencyIncidents");

            migrationBuilder.DropTable(
                name: "TelemetryPings");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
