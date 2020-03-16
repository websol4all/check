using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class ChangetypeofCompanyidindDeviceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "DeviceHistory",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceHistory",
                table: "DeviceHistory",
                columns: new[] { "DeviceId", "CompanyId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_DeviceHistory",
                table: "DeviceHistory");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "DeviceHistory");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "DeviceHistory",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_DeviceHistory",
                table: "DeviceHistory",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceHistory_DeviceId",
                table: "DeviceHistory",
                column: "DeviceId");
        }
    }
}
