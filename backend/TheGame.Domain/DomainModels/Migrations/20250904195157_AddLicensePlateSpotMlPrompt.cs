using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheGame.Domain.DomainModels.Migrations
{
    /// <inheritdoc />
    public partial class AddLicensePlateSpotMlPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicensePlateSpotMlPrompts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    SpottedByPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: false),
                    MlPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePlateSpotMlPrompts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LicensePlateSpotMlPrompts_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LicensePlateSpotMlPrompts_LicensePlate_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LicensePlateSpotMlPrompts_Players_SpottedByPlayerId",
                        column: x => x.SpottedByPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateSpotMlPrompts_GameId_LicensePlateId",
                table: "LicensePlateSpotMlPrompts",
                columns: new[] { "GameId", "LicensePlateId" });

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateSpotMlPrompts_LicensePlateId",
                table: "LicensePlateSpotMlPrompts",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlateSpotMlPrompts_SpottedByPlayerId",
                table: "LicensePlateSpotMlPrompts",
                column: "SpottedByPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicensePlateSpotMlPrompts");
        }
    }
}
