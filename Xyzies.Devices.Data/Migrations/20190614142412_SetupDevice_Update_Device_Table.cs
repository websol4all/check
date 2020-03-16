using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class SetupDevice_Update_Device_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HexnodeUdid",
                table: "Devices",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPending",
                table: "Devices",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HexnodeUdid",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsPending",
                table: "Devices");
        }
    }
}
