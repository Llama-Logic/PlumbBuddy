using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumbBuddy.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilesOfInterest",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilesOfInterest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModCreators",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModCreators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModFileHashes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileHashes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModManifests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModManifests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntentionalOverrides",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModManfiestId = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    ModName = table.Column<string>(type: "TEXT", nullable: true),
                    ModVersion = table.Column<string>(type: "TEXT", nullable: true),
                    ModManifestKeyType = table.Column<int>(type: "INTEGER", nullable: true),
                    ModManifestKeyGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    ModManifestKeyFullInstance = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntentionalOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntentionalOverrides_ModManifests_ModManfiestId",
                        column: x => x.ModManfiestId,
                        principalTable: "ModManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModCreatorModManifest",
                columns: table => new
                {
                    AttributedModsId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatorsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModCreatorModManifest", x => new { x.AttributedModsId, x.CreatorsId });
                    table.ForeignKey(
                        name: "FK_ModCreatorModManifest_ModCreators_CreatorsId",
                        column: x => x.CreatorsId,
                        principalTable: "ModCreators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModCreatorModManifest_ModManifests_AttributedModsId",
                        column: x => x.AttributedModsId,
                        principalTable: "ModManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFileHashModManifest",
                columns: table => new
                {
                    ModManifestSubsumedFilesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SubsumedFilesId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileHashModManifest", x => new { x.ModManifestSubsumedFilesId, x.SubsumedFilesId });
                    table.ForeignKey(
                        name: "FK_ModFileHashModManifest_ModFileHashes_SubsumedFilesId",
                        column: x => x.SubsumedFilesId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileHashModManifest_ModManifests_ModManifestSubsumedFilesId",
                        column: x => x.ModManifestSubsumedFilesId,
                        principalTable: "ModManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModManifestId = table.Column<long>(type: "INTEGER", nullable: true),
                    Path = table.Column<string>(type: "TEXT", nullable: true),
                    Creation = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LastWrite = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: true),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false),
                    AbsenceNoticed = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFiles_ModFileHashes_ModFileHashId",
                        column: x => x.ModFileHashId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFiles_ModManifests_ModManifestId",
                        column: x => x.ModManifestId,
                        principalTable: "ModManifests",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RequiredMods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModManfiestId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    ManifestKeyType = table.Column<int>(type: "INTEGER", nullable: true),
                    ManifestKeyGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    ManifestKeyFullInstance = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequiredMods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequiredMods_ModManifests_ModManfiestId",
                        column: x => x.ModManfiestId,
                        principalTable: "ModManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntentionalOverrideModFileHash",
                columns: table => new
                {
                    IntentionalOverridesModFilesId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModFilesId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntentionalOverrideModFileHash", x => new { x.IntentionalOverridesModFilesId, x.ModFilesId });
                    table.ForeignKey(
                        name: "FK_IntentionalOverrideModFileHash_IntentionalOverrides_IntentionalOverridesModFilesId",
                        column: x => x.IntentionalOverridesModFilesId,
                        principalTable: "IntentionalOverrides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IntentionalOverrideModFileHash_ModFileHashes_ModFilesId",
                        column: x => x.ModFilesId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFileResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileId = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFileResources_ModFiles_ModFileId",
                        column: x => x.ModFileId,
                        principalTable: "ModFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModCreatorRequiredMod",
                columns: table => new
                {
                    AttributedRequiredModsId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatorsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModCreatorRequiredMod", x => new { x.AttributedRequiredModsId, x.CreatorsId });
                    table.ForeignKey(
                        name: "FK_ModCreatorRequiredMod_ModCreators_CreatorsId",
                        column: x => x.CreatorsId,
                        principalTable: "ModCreators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModCreatorRequiredMod_RequiredMods_AttributedRequiredModsId",
                        column: x => x.AttributedRequiredModsId,
                        principalTable: "RequiredMods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFileHashRequiredMod",
                columns: table => new
                {
                    FilesId = table.Column<long>(type: "INTEGER", nullable: false),
                    RequiredModFilesId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileHashRequiredMod", x => new { x.FilesId, x.RequiredModFilesId });
                    table.ForeignKey(
                        name: "FK_ModFileHashRequiredMod_ModFileHashes_FilesId",
                        column: x => x.FilesId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileHashRequiredMod_RequiredMods_RequiredModFilesId",
                        column: x => x.RequiredModFilesId,
                        principalTable: "RequiredMods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilesOfInterest_Path",
                table: "FilesOfInterest",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_IntentionalOverrideModFileHash_ModFilesId",
                table: "IntentionalOverrideModFileHash",
                column: "ModFilesId");

            migrationBuilder.CreateIndex(
                name: "IX_IntentionalOverrides_ModManfiestId",
                table: "IntentionalOverrides",
                column: "ModManfiestId");

            migrationBuilder.CreateIndex(
                name: "IX_ModCreatorModManifest_CreatorsId",
                table: "ModCreatorModManifest",
                column: "CreatorsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModCreatorRequiredMod_CreatorsId",
                table: "ModCreatorRequiredMod",
                column: "CreatorsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModCreators_Name",
                table: "ModCreators",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashes_Sha256",
                table: "ModFileHashes",
                column: "Sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashModManifest_SubsumedFilesId",
                table: "ModFileHashModManifest",
                column: "SubsumedFilesId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashRequiredMod_RequiredModFilesId",
                table: "ModFileHashRequiredMod",
                column: "RequiredModFilesId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileResources_ModFileId",
                table: "ModFileResources",
                column: "ModFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_ModFileHashId",
                table: "ModFiles",
                column: "ModFileHashId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_ModManifestId",
                table: "ModFiles",
                column: "ModManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_Path",
                table: "ModFiles",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_Path_Creation_LastWrite_Size",
                table: "ModFiles",
                columns: new[] { "Path", "Creation", "LastWrite", "Size" });

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_ModManfiestId",
                table: "RequiredMods",
                column: "ModManfiestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilesOfInterest");

            migrationBuilder.DropTable(
                name: "IntentionalOverrideModFileHash");

            migrationBuilder.DropTable(
                name: "ModCreatorModManifest");

            migrationBuilder.DropTable(
                name: "ModCreatorRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFileHashModManifest");

            migrationBuilder.DropTable(
                name: "ModFileHashRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFileResources");

            migrationBuilder.DropTable(
                name: "IntentionalOverrides");

            migrationBuilder.DropTable(
                name: "ModCreators");

            migrationBuilder.DropTable(
                name: "RequiredMods");

            migrationBuilder.DropTable(
                name: "ModFiles");

            migrationBuilder.DropTable(
                name: "ModFileHashes");

            migrationBuilder.DropTable(
                name: "ModManifests");
        }
    }
}
