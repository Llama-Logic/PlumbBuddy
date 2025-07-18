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
    public partial class ModelV8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameStringTableEntries_GameStringsPackageResources_GameStringsPackageResourceId",
                table: "GameStringTableEntries");

            migrationBuilder.DropTable(
                name: "GameStringsPackageResources");

            migrationBuilder.DropTable(
                name: "GameStringsPackages");

            migrationBuilder.RenameColumn(
                name: "GameStringsPackageResourceId",
                table: "GameStringTableEntries",
                newName: "GameResourcePackageResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_GameStringTableEntries_GameStringsPackageResourceId_SignedKey",
                table: "GameStringTableEntries",
                newName: "IX_GameStringTableEntries_GameResourcePackageResourceId_SignedKey");

            migrationBuilder.CreateTable(
                name: "GameResourcePackages",
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
                    table.PrimaryKey("PK_GameResourcePackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameResourcePackageResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameResourcePackageId = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false),
                    StringTableLocalePrefix = table.Column<byte>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameResourcePackageResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameResourcePackageResources_GameResourcePackages_GameResourcePackageId",
                        column: x => x.GameResourcePackageId,
                        principalTable: "GameResourcePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameResourcePackageResources_GameResourcePackageId_KeyType_KeyGroup_KeyFullInstance",
                table: "GameResourcePackageResources",
                columns: new[] { "GameResourcePackageId", "KeyType", "KeyGroup", "KeyFullInstance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameResourcePackages_Path",
                table: "GameResourcePackages",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameResourcePackages_Path_Creation_LastWrite_Size",
                table: "GameResourcePackages",
                columns: new[] { "Path", "Creation", "LastWrite", "Size" });

            migrationBuilder.AddForeignKey(
                name: "FK_GameStringTableEntries_GameResourcePackageResources_GameResourcePackageResourceId",
                table: "GameStringTableEntries",
                column: "GameResourcePackageResourceId",
                principalTable: "GameResourcePackageResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GameStringTableEntries_GameResourcePackageResources_GameResourcePackageResourceId",
                table: "GameStringTableEntries");

            migrationBuilder.DropTable(
                name: "GameResourcePackageResources");

            migrationBuilder.DropTable(
                name: "GameResourcePackages");

            migrationBuilder.RenameColumn(
                name: "GameResourcePackageResourceId",
                table: "GameStringTableEntries",
                newName: "GameStringsPackageResourceId");

            migrationBuilder.RenameIndex(
                name: "IX_GameStringTableEntries_GameResourcePackageResourceId_SignedKey",
                table: "GameStringTableEntries",
                newName: "IX_GameStringTableEntries_GameStringsPackageResourceId_SignedKey");

            migrationBuilder.CreateTable(
                name: "GameStringsPackages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Creation = table.Column<long>(type: "INTEGER", nullable: false),
                    IsDelta = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastWrite = table.Column<long>(type: "INTEGER", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false)
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
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
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

            migrationBuilder.AddForeignKey(
                name: "FK_GameStringTableEntries_GameStringsPackageResources_GameStringsPackageResourceId",
                table: "GameStringTableEntries",
                column: "GameStringsPackageResourceId",
                principalTable: "GameStringsPackageResources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
