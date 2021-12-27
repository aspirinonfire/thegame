using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheGame.Domain.DAL.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LicensePlates",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StateOrProvince = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LicensePlates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "LicensePlates",
                columns: new[] { "Id", "Country", "StateOrProvince" },
                values: new object[,]
                {
                    { 1L, "CA", "BC" },
                    { 2L, "US", "AK" },
                    { 3L, "US", "CA" },
                    { 4L, "US", "NV" },
                    { 5L, "US", "OR" },
                    { 6L, "US", "WA" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LicensePlates");
        }
    }
}
