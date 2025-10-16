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
    public partial class ModelV15 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModFilePlayerRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    PersonalDate = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFilePlayerRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModFileHashModFilePlayerRecord",
                columns: table => new
                {
                    ModFileHashesId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModFilePlayerRecordsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileHashModFilePlayerRecord", x => new { x.ModFileHashesId, x.ModFilePlayerRecordsId });
                    table.ForeignKey(
                        name: "FK_ModFileHashModFilePlayerRecord_ModFileHashes_ModFileHashesId",
                        column: x => x.ModFileHashesId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileHashModFilePlayerRecord_ModFilePlayerRecords_ModFilePlayerRecordsId",
                        column: x => x.ModFilePlayerRecordsId,
                        principalTable: "ModFilePlayerRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFilePlayerRecordPaths",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFilePlayerRecordId = table.Column<long>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFilePlayerRecordPaths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFilePlayerRecordPaths_ModFilePlayerRecords_ModFilePlayerRecordId",
                        column: x => x.ModFilePlayerRecordId,
                        principalTable: "ModFilePlayerRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashModFilePlayerRecord_ModFilePlayerRecordsId",
                table: "ModFileHashModFilePlayerRecord",
                column: "ModFilePlayerRecordsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFilePlayerRecordPaths_ModFilePlayerRecordId_Path",
                table: "ModFilePlayerRecordPaths",
                columns: new[] { "ModFilePlayerRecordId", "Path" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModFileHashModFilePlayerRecord");

            migrationBuilder.DropTable(
                name: "ModFilePlayerRecordPaths");

            migrationBuilder.DropTable(
                name: "ModFilePlayerRecords");
        }
    }
}
