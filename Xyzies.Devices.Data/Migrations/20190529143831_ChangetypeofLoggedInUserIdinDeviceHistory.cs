using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class ChangetypeofLoggedInUserIdinDeviceHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "LoggedInUserId",
                table: "DeviceHistory",
                nullable: true,
                oldClrType: typeof(Guid));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "LoggedInUserId",
                table: "DeviceHistory",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);
        }
    }
}
