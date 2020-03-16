using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class initializeDbContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Udid = table.Column<string>(nullable: false),
                    Latitude = table.Column<double>(nullable: false),
                    Longitude = table.Column<double>(nullable: false),
                    Radius = table.Column<double>(nullable: false),
                    CompanyId = table.Column<int>(nullable: false),
                    BranchId = table.Column<Guid>(nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false),
                    IsOnline = table.Column<bool>(nullable: false),
                    IsInLocation = table.Column<bool>(nullable: false),
                    CurrentDeviceLocationLatitude = table.Column<double>(nullable: false),
                    CurrentDeviceLocationLongitude = table.Column<double>(nullable: false),
                    DeviceLocationLatitude = table.Column<double>(nullable: false),
                    DeviceLocationLongitude = table.Column<double>(nullable: false),
                    DeviceRadius = table.Column<double>(nullable: false),
                    LoggedInUserId = table.Column<Guid>(nullable: false),
                    DeviceId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceHistory_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistory_DeviceId",
                table: "DeviceHistory",
                column: "DeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceHistory");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
