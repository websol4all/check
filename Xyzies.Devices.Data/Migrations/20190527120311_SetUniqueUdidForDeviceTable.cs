using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class SetUniqueUdidForDeviceTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Udid",
                table: "Devices",
                nullable: false,
                oldClrType: typeof(string));

            migrationBuilder.CreateIndex(
                name: "IX_Devices_Udid",
                table: "Devices",
                column: "Udid",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Devices_Udid",
                table: "Devices");

            migrationBuilder.AlterColumn<string>(
                name: "Udid",
                table: "Devices",
                nullable: false,
                oldClrType: typeof(string));
        }
    }
}
