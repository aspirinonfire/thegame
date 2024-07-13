using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TheGame.Domain.DomainModels.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicensePlates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateOrProvince = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePlates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerIdentities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderIdentityId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlayerIdentityId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_PlayerIdentities_PlayerIdentityId",
                        column: x => x.PlayerIdentityId,
                        principalTable: "PlayerIdentities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedByPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    EndedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DateCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    GameScore_Achievements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GameScore_TotalScore = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Players_CreatedByPlayerId",
                        column: x => x.CreatedByPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameLicensePlates",
                columns: table => new
                {
                    LicensePlateId = table.Column<long>(type: "bigint", nullable: false),
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    SpottedByPlayerId = table.Column<long>(type: "bigint", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameLicensePlates", x => new { x.GameId, x.LicensePlateId });
                    table.ForeignKey(
                        name: "FK_GameLicensePlates_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameLicensePlates_LicensePlates_LicensePlateId",
                        column: x => x.LicensePlateId,
                        principalTable: "LicensePlates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameLicensePlates_Players_SpottedByPlayerId",
                        column: x => x.SpottedByPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GamePlayer",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    GameId = table.Column<long>(type: "bigint", nullable: false),
                    InvitationToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InviteStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DateModified = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GamePlayer", x => new { x.GameId, x.PlayerId });
                    table.ForeignKey(
                        name: "FK_GamePlayer_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GamePlayer_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "LicensePlates",
                columns: new[] { "Id", "Country", "StateOrProvince" },
                values: new object[,]
                {
                    { 1L, "US", "AL" },
                    { 2L, "US", "AK" },
                    { 3L, "US", "AZ" },
                    { 4L, "US", "AR" },
                    { 5L, "US", "CA" },
                    { 6L, "US", "CO" },
                    { 7L, "US", "CT" },
                    { 8L, "US", "DE" },
                    { 9L, "US", "DC" },
                    { 10L, "US", "FL" },
                    { 11L, "US", "GA" },
                    { 12L, "US", "HI" },
                    { 13L, "US", "ID" },
                    { 14L, "US", "IL" },
                    { 15L, "US", "IN" },
                    { 16L, "US", "IA" },
                    { 17L, "US", "KS" },
                    { 18L, "US", "KY" },
                    { 19L, "US", "LA" },
                    { 20L, "US", "ME" },
                    { 21L, "US", "MD" },
                    { 22L, "US", "MA" },
                    { 23L, "US", "MI" },
                    { 24L, "US", "MN" },
                    { 25L, "US", "MS" },
                    { 26L, "US", "MO" },
                    { 27L, "US", "MT" },
                    { 28L, "US", "NE" },
                    { 29L, "US", "NV" },
                    { 30L, "US", "NH" },
                    { 31L, "US", "NJ" },
                    { 32L, "US", "NM" },
                    { 33L, "US", "NY" },
                    { 34L, "US", "NC" },
                    { 35L, "US", "ND" },
                    { 36L, "US", "OH" },
                    { 37L, "US", "OK" },
                    { 38L, "US", "OR" },
                    { 39L, "US", "PA" },
                    { 40L, "US", "RI" },
                    { 41L, "US", "SC" },
                    { 42L, "US", "SD" },
                    { 43L, "US", "TN" },
                    { 44L, "US", "TX" },
                    { 45L, "US", "UT" },
                    { 46L, "US", "VT" },
                    { 47L, "US", "VA" },
                    { 48L, "US", "WA" },
                    { 49L, "US", "WV" },
                    { 50L, "US", "WI" },
                    { 51L, "US", "WY" },
                    { 52L, "CA", "AB" },
                    { 53L, "CA", "BC" },
                    { 54L, "CA", "MB" },
                    { 55L, "CA", "NB" },
                    { 56L, "CA", "NL" },
                    { 57L, "CA", "NT" },
                    { 58L, "CA", "NS" },
                    { 59L, "CA", "NU" },
                    { 60L, "CA", "ON" },
                    { 61L, "CA", "PE" },
                    { 62L, "CA", "QC" },
                    { 63L, "CA", "SK" },
                    { 64L, "CA", "YT" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameLicensePlates_LicensePlateId",
                table: "GameLicensePlates",
                column: "LicensePlateId");

            migrationBuilder.CreateIndex(
                name: "IX_GameLicensePlates_SpottedByPlayerId",
                table: "GameLicensePlates",
                column: "SpottedByPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_GamePlayer_PlayerId",
                table: "GamePlayer",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CreatedByPlayerId",
                table: "Games",
                column: "CreatedByPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_LicensePlates_Country_StateOrProvince",
                table: "LicensePlates",
                columns: new[] { "Country", "StateOrProvince" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerIdentities_ProviderName_ProviderIdentityId",
                table: "PlayerIdentities",
                columns: new[] { "ProviderName", "ProviderIdentityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerIdentityId",
                table: "Players",
                column: "PlayerIdentityId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameLicensePlates");

            migrationBuilder.DropTable(
                name: "GamePlayer");

            migrationBuilder.DropTable(
                name: "LicensePlates");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "PlayerIdentities");
        }
    }
}
