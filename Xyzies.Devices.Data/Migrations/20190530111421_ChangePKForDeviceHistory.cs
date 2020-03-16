using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class ChangePKForDeviceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceHistory",
                table: "DeviceHistory");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "DeviceHistory",
                nullable: false,
                defaultValue: Guid.NewGuid());

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceHistory",
                table: "DeviceHistory",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistory_DeviceId",
                table: "DeviceHistory",
                column: "DeviceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceHistory",
                table: "DeviceHistory");

            migrationBuilder.DropIndex(
                name: "IX_DeviceHistory_DeviceId",
                table: "DeviceHistory");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "DeviceHistory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceHistory",
                table: "DeviceHistory",
                columns: new[] { "DeviceId", "CompanyId" });
        }
    }
}
