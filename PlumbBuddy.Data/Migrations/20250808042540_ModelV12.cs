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
    public partial class ModelV12 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModFileExcludedEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileManifestId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileExcludedEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFileExcludedEntries_ModFileManifests_ModFileManifestId",
                        column: x => x.ModFileManifestId,
                        principalTable: "ModFileManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModFileExcludedEntries_ModFileManifestId",
                table: "ModFileExcludedEntries",
                column: "ModFileManifestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModFileExcludedEntries");
        }
    }
}
