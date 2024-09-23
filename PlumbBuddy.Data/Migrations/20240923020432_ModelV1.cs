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
                name: "ModFeatures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFeatures", x => x.Id);
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
                name: "TopologySnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Taken = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopologySnapshots", x => x.Id);
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
                name: "ModFeatureModManifest",
                columns: table => new
                {
                    FeaturesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SpecifiedByModManifestsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFeatureModManifest", x => new { x.FeaturesId, x.SpecifiedByModManifestsId });
                    table.ForeignKey(
                        name: "FK_ModFeatureModManifest_ModFeatures_FeaturesId",
                        column: x => x.FeaturesId,
                        principalTable: "ModFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFeatureModManifest_ModManifests_SpecifiedByModManifestsId",
                        column: x => x.SpecifiedByModManifestsId,
                        principalTable: "ModManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "ModFeatureRequiredMod",
                columns: table => new
                {
                    RequiredFeaturesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SpecifiedByRequiredModsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFeatureRequiredMod", x => new { x.RequiredFeaturesId, x.SpecifiedByRequiredModsId });
                    table.ForeignKey(
                        name: "FK_ModFeatureRequiredMod_ModFeatures_RequiredFeaturesId",
                        column: x => x.RequiredFeaturesId,
                        principalTable: "ModFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFeatureRequiredMod_RequiredMods_SpecifiedByRequiredModsId",
                        column: x => x.SpecifiedByRequiredModsId,
                        principalTable: "RequiredMods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFileHashes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    ResourcesAndManifestCataloged = table.Column<bool>(type: "INTEGER", nullable: false),
                    ModManifestId = table.Column<long>(type: "INTEGER", nullable: true),
                    IntentionalOverrideId = table.Column<long>(type: "INTEGER", nullable: true),
                    RequiredModId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileHashes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFileHashes_IntentionalOverrides_IntentionalOverrideId",
                        column: x => x.IntentionalOverrideId,
                        principalTable: "IntentionalOverrides",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModFileHashes_ModManifests_ModManifestId",
                        column: x => x.ModManifestId,
                        principalTable: "ModManifests",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModFileHashes_RequiredMods_RequiredModId",
                        column: x => x.RequiredModId,
                        principalTable: "RequiredMods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ModFileResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFileResources_ModFileHashes_ModFileHashId",
                        column: x => x.ModFileHashId,
                        principalTable: "ModFileHashes",
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
                });

            migrationBuilder.CreateTable(
                name: "ScriptModArchiveEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    CompressedLength = table.Column<long>(type: "INTEGER", nullable: false),
                    SignedCrc32 = table.Column<int>(type: "INTEGER", nullable: false),
                    ExternalAttributes = table.Column<int>(type: "INTEGER", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    IsEncrypted = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastWriteTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Length = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptModArchiveEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScriptModArchiveEntries_ModFileHashes_ModFileHashId",
                        column: x => x.ModFileHashId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFileResourceTopologySnapshot",
                columns: table => new
                {
                    ResourcesId = table.Column<long>(type: "INTEGER", nullable: false),
                    TopologySnapshotsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileResourceTopologySnapshot", x => new { x.ResourcesId, x.TopologySnapshotsId });
                    table.ForeignKey(
                        name: "FK_ModFileResourceTopologySnapshot_ModFileResources_ResourcesId",
                        column: x => x.ResourcesId,
                        principalTable: "ModFileResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileResourceTopologySnapshot_TopologySnapshots_TopologySnapshotsId",
                        column: x => x.TopologySnapshotsId,
                        principalTable: "TopologySnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilesOfInterest_Path",
                table: "FilesOfInterest",
                column: "Path",
                unique: true);

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
                name: "IX_ModFeatureModManifest_SpecifiedByModManifestsId",
                table: "ModFeatureModManifest",
                column: "SpecifiedByModManifestsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFeatureRequiredMod_SpecifiedByRequiredModsId",
                table: "ModFeatureRequiredMod",
                column: "SpecifiedByRequiredModsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFeatures_Name",
                table: "ModFeatures",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashes_IntentionalOverrideId",
                table: "ModFileHashes",
                column: "IntentionalOverrideId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashes_ModManifestId",
                table: "ModFileHashes",
                column: "ModManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashes_RequiredModId",
                table: "ModFileHashes",
                column: "RequiredModId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileHashes_Sha256",
                table: "ModFileHashes",
                column: "Sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModFileResources_ModFileHashId",
                table: "ModFileResources",
                column: "ModFileHashId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileResourceTopologySnapshot_TopologySnapshotsId",
                table: "ModFileResourceTopologySnapshot",
                column: "TopologySnapshotsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_ModFileHashId",
                table: "ModFiles",
                column: "ModFileHashId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_Path",
                table: "ModFiles",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_Path_Creation_LastWrite_Size",
                table: "ModFiles",
                columns: new[] { "Path", "Creation", "LastWrite", "Size" });

            migrationBuilder.CreateIndex(
                name: "IX_ModManifestPackCode_RequiredPacksId",
                table: "ModManifestPackCode",
                column: "RequiredPacksId");

            migrationBuilder.CreateIndex(
                name: "IX_PackCodes_Code",
                table: "PackCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_ModManfiestId",
                table: "RequiredMods",
                column: "ModManfiestId");

            migrationBuilder.CreateIndex(
                name: "IX_ScriptModArchiveEntries_ModFileHashId_FullName",
                table: "ScriptModArchiveEntries",
                columns: new[] { "ModFileHashId", "FullName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilesOfInterest");

            migrationBuilder.DropTable(
                name: "ModCreatorModManifest");

            migrationBuilder.DropTable(
                name: "ModCreatorRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFeatureModManifest");

            migrationBuilder.DropTable(
                name: "ModFeatureRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFileResourceTopologySnapshot");

            migrationBuilder.DropTable(
                name: "ModFiles");

            migrationBuilder.DropTable(
                name: "ModManifestPackCode");

            migrationBuilder.DropTable(
                name: "ScriptModArchiveEntries");

            migrationBuilder.DropTable(
                name: "ModCreators");

            migrationBuilder.DropTable(
                name: "ModFeatures");

            migrationBuilder.DropTable(
                name: "ModFileResources");

            migrationBuilder.DropTable(
                name: "TopologySnapshots");

            migrationBuilder.DropTable(
                name: "PackCodes");

            migrationBuilder.DropTable(
                name: "ModFileHashes");

            migrationBuilder.DropTable(
                name: "IntentionalOverrides");

            migrationBuilder.DropTable(
                name: "RequiredMods");

            migrationBuilder.DropTable(
                name: "ModManifests");
        }
    }
}
