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
    public partial class ModelV7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameStringsPackages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Creation = table.Column<long>(type: "INTEGER", nullable: false),
                    LastWrite = table.Column<long>(type: "INTEGER", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    IsDelta = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStringsPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameStringsPackageResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameStringsPackageId = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false),
                    StringTableLocalePrefix = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStringsPackageResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameStringsPackageResources_GameStringsPackages_GameStringsPackageId",
                        column: x => x.GameStringsPackageId,
                        principalTable: "GameStringsPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameStringTableEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameStringsPackageResourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    SignedKey = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStringTableEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameStringTableEntries_GameStringsPackageResources_GameStringsPackageResourceId",
                        column: x => x.GameStringsPackageResourceId,
                        principalTable: "GameStringsPackageResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameStringsPackageResources_GameStringsPackageId_KeyType_KeyGroup_KeyFullInstance",
                table: "GameStringsPackageResources",
                columns: new[] { "GameStringsPackageId", "KeyType", "KeyGroup", "KeyFullInstance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameStringsPackages_Path",
                table: "GameStringsPackages",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameStringsPackages_Path_Creation_LastWrite_Size",
                table: "GameStringsPackages",
                columns: new[] { "Path", "Creation", "LastWrite", "Size" });

            migrationBuilder.CreateIndex(
                name: "IX_GameStringTableEntries_GameStringsPackageResourceId_SignedKey",
                table: "GameStringTableEntries",
                columns: new[] { "GameStringsPackageResourceId", "SignedKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameStringTableEntries");

            migrationBuilder.DropTable(
                name: "GameStringsPackageResources");

            migrationBuilder.DropTable(
                name: "GameStringsPackages");
        }
    }
}
