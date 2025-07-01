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
    public partial class ModelV5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModFileResources_ModFileHashId",
                table: "ModFileResources");

            migrationBuilder.AddColumn<byte>(
                name: "StringTableLocalePrefix",
                table: "ModFileResources",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<bool>(
                name: "StringTablesCataloged",
                table: "ModFileHashes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ModFileStringTableEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileResourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    SignedKey = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileStringTableEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFileStringTableEntries_ModFileResources_ModFileResourceId",
                        column: x => x.ModFileResourceId,
                        principalTable: "ModFileResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModFileResources_ModFileHashId_KeyType_KeyGroup_KeyFullInstance",
                table: "ModFileResources",
                columns: new[] { "ModFileHashId", "KeyType", "KeyGroup", "KeyFullInstance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModFileStringTableEntries_ModFileResourceId_SignedKey",
                table: "ModFileStringTableEntries",
                columns: new[] { "ModFileResourceId", "SignedKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModFileStringTableEntries");

            migrationBuilder.DropIndex(
                name: "IX_ModFileResources_ModFileHashId_KeyType_KeyGroup_KeyFullInstance",
                table: "ModFileResources");

            migrationBuilder.DropColumn(
                name: "StringTableLocalePrefix",
                table: "ModFileResources");

            migrationBuilder.DropColumn(
                name: "StringTablesCataloged",
                table: "ModFileHashes");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileResources_ModFileHashId",
                table: "ModFileResources",
                column: "ModFileHashId");
        }
    }
}
