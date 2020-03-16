using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class AddNewColoumnToDevice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "Devices",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "Devices");
        }
    }
}
