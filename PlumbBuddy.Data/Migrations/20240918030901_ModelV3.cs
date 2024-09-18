using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumbBuddy.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModManifestPackCode",
                columns: table => new
                {
                    RequiredByModsId = table.Column<long>(type: "INTEGER", nullable: false),
                    RequiredPacksId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModManifestPackCode", x => new { x.RequiredByModsId, x.RequiredPacksId });
                    table.ForeignKey(
                        name: "FK_ModManifestPackCode_ModManifests_RequiredByModsId",
                        column: x => x.RequiredByModsId,
                        principalTable: "ModManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModManifestPackCode_PackCodes_RequiredPacksId",
                        column: x => x.RequiredPacksId,
                        principalTable: "PackCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModManifestPackCode_RequiredPacksId",
                table: "ModManifestPackCode",
                column: "RequiredPacksId");

            migrationBuilder.CreateIndex(
                name: "IX_PackCodes_Code",
                table: "PackCodes",
                column: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModManifestPackCode");

            migrationBuilder.DropTable(
                name: "PackCodes");
        }
    }
}
