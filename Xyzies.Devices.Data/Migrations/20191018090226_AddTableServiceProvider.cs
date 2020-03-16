using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Xyzies.Devices.Data.Migrations
{
    public partial class AddTableServiceProvider : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServiceProviderId",
                table: "Devices",
                nullable: false,
                defaultValue: new Guid("0ed21401-e0e6-4b22-aa89-4c5522212b67"));

            migrationBuilder.CreateTable(
                name: "ServiceProviders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false),
                    SeviceProviderName = table.Column<string>(nullable: false),
                    Phone = table.Column<string>(nullable: false),
                    CreatedOn = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviders", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ServiceProviders",
                columns: new[] { "Id", "CreatedOn", "IsDeleted", "Phone", "SeviceProviderName" },
                values: new object[] { new Guid("0ed21401-e0e6-4b22-aa89-4c5522212b67"), new DateTime(2019, 10, 18, 9, 2, 26, 359, DateTimeKind.Utc).AddTicks(4102), false, "380938821599", "Spectrum" });

            migrationBuilder.CreateIndex(
                name: "IX_Devices_ServiceProviderId",
                table: "Devices",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_SeviceProviderName",
                table: "ServiceProviders",
                column: "SeviceProviderName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Devices_ServiceProviders_ServiceProviderId",
                table: "Devices",
                column: "ServiceProviderId",
                principalTable: "ServiceProviders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Devices_ServiceProviders_ServiceProviderId",
                table: "Devices");

            migrationBuilder.DropTable(
                name: "ServiceProviders");

            migrationBuilder.DropIndex(
                name: "IX_Devices_ServiceProviderId",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "ServiceProviderId",
                table: "Devices");
        }
    }
}
