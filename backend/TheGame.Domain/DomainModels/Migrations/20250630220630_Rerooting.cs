using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheGame.Domain.DomainModels.Migrations
{
    /// <inheritdoc />
    public partial class Rerooting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_PlayerIdentityId",
                table: "Players");

            migrationBuilder.AlterColumn<long>(
                name: "PlayerIdentityId",
                table: "Players",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerIdentityId",
                table: "Players",
                column: "PlayerIdentityId",
                unique: true,
                filter: "[PlayerIdentityId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Players_PlayerIdentityId",
                table: "Players");

            migrationBuilder.AlterColumn<long>(
                name: "PlayerIdentityId",
                table: "Players",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerIdentityId",
                table: "Players",
                column: "PlayerIdentityId",
                unique: true);
        }
    }
}
