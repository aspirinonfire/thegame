using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheGame.Domain.DomainModels.Migrations
{
    /// <inheritdoc />
    public partial class userIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PlayerIdentityId",
                table: "Players",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

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

            migrationBuilder.CreateIndex(
                name: "IX_Players_PlayerIdentityId",
                table: "Players",
                column: "PlayerIdentityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerIdentities_ProviderName_ProviderIdentityId",
                table: "PlayerIdentities",
                columns: new[] { "ProviderName", "ProviderIdentityId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Players_PlayerIdentities_PlayerIdentityId",
                table: "Players",
                column: "PlayerIdentityId",
                principalTable: "PlayerIdentities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_PlayerIdentities_PlayerIdentityId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "PlayerIdentities");

            migrationBuilder.DropIndex(
                name: "IX_Players_PlayerIdentityId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PlayerIdentityId",
                table: "Players");
        }
    }
}
