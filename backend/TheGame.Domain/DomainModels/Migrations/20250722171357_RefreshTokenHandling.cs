using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheGame.Domain.DomainModels.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "PlayerIdentities");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiration",
                table: "PlayerIdentities");

            migrationBuilder.AddColumn<bool>(
                name: "IsDisabled",
                table: "PlayerIdentities",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDisabled",
                table: "PlayerIdentities");

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "PlayerIdentities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefreshTokenExpiration",
                table: "PlayerIdentities",
                type: "datetimeoffset",
                nullable: true);
        }
    }
}
