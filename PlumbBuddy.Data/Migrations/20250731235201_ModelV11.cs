using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0079
#pragma warning disable CA1062
#pragma warning disable CA1861
#pragma warning disable IDE0053
#pragma warning disable IDE0161
#pragma warning disable IDE0300

namespace PlumbBuddy.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelV11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecommendedPacks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileManfiestId = table.Column<long>(type: "INTEGER", nullable: false),
                    PackCodeId = table.Column<long>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendedPacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendedPacks_ModFileManifests_ModFileManfiestId",
                        column: x => x.ModFileManfiestId,
                        principalTable: "ModFileManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecommendedPacks_PackCodes_PackCodeId",
                        column: x => x.PackCodeId,
                        principalTable: "PackCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecommendedPacks_ModFileManfiestId",
                table: "RecommendedPacks",
                column: "ModFileManfiestId");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendedPacks_PackCodeId",
                table: "RecommendedPacks",
                column: "PackCodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendedPacks");
        }
    }
}
