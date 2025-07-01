using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheGame.Domain.DomainModels.Migrations
{
    /// <inheritdoc />
    public partial class MoreRerooting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameLicensePlates_LicensePlates_LicensePlateId",
                table: "GameLicensePlates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LicensePlates",
                table: "LicensePlates");

            migrationBuilder.RenameTable(
                name: "LicensePlates",
                newName: "LicensePlate");

            migrationBuilder.RenameIndex(
                name: "IX_LicensePlates_Country_StateOrProvince",
                table: "LicensePlate",
                newName: "IX_LicensePlate_Country_StateOrProvince");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LicensePlate",
                table: "LicensePlate",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameLicensePlates_LicensePlate_LicensePlateId",
                table: "GameLicensePlates",
                column: "LicensePlateId",
                principalTable: "LicensePlate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameLicensePlates_LicensePlate_LicensePlateId",
                table: "GameLicensePlates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LicensePlate",
                table: "LicensePlate");

            migrationBuilder.RenameTable(
                name: "LicensePlate",
                newName: "LicensePlates");

            migrationBuilder.RenameIndex(
                name: "IX_LicensePlate_Country_StateOrProvince",
                table: "LicensePlates",
                newName: "IX_LicensePlates_Country_StateOrProvince");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LicensePlates",
                table: "LicensePlates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameLicensePlates_LicensePlates_LicensePlateId",
                table: "GameLicensePlates",
                column: "LicensePlateId",
                principalTable: "LicensePlates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
