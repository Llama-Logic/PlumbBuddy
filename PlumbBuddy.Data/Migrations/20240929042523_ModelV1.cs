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
                name: "ModExclusivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModExclusivities", x => x.Id);
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
                name: "ModFileHashes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    ResourcesAndManifestCataloged = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileHashes", x => x.Id);
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
                name: "RequirementIdentifiers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementIdentifiers", x => x.Id);
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

            migrationBuilder.CreateTable(
                name: "HashResourceKeys",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModManifestId = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashResourceKeys", x => x.Id);
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
                    ModManifestKeyFullInstance = table.Column<long>(type: "INTEGER", nullable: true),
                    ModManifestHashId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntentionalOverrides", x => x.Id);
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
                });

            migrationBuilder.CreateTable(
                name: "ModExclusivityModManifest",
                columns: table => new
                {
                    ExclusivitiesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SpecifiedByModManifestsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModExclusivityModManifest", x => new { x.ExclusivitiesId, x.SpecifiedByModManifestsId });
                    table.ForeignKey(
                        name: "FK_ModExclusivityModManifest_ModExclusivities_ExclusivitiesId",
                        column: x => x.ExclusivitiesId,
                        principalTable: "ModExclusivities",
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
                });

            migrationBuilder.CreateTable(
                name: "ModManifestHashes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    ModManifestId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModManifestHashes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModManifests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    InscribedModManifestHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    ResourceHashStrategy = table.Column<int>(type: "INTEGER", nullable: true),
                    CalculatedModManifestHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    TuningName = table.Column<string>(type: "TEXT", nullable: true),
                    TuningFullInstance = table.Column<long>(type: "INTEGER", nullable: true),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: true),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModManifests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModManifests_ModFileHashes_ModFileHashId",
                        column: x => x.ModFileHashId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModManifests_ModManifestHashes_CalculatedModManifestHashId",
                        column: x => x.CalculatedModManifestHashId,
                        principalTable: "ModManifestHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModManifests_ModManifestHashes_InscribedModManifestHashId",
                        column: x => x.InscribedModManifestHashId,
                        principalTable: "ModManifestHashes",
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
                    ManifestKeyFullInstance = table.Column<long>(type: "INTEGER", nullable: true),
                    RequirementIdentifierId = table.Column<long>(type: "INTEGER", nullable: true),
                    IgnoreIfHashAvailableId = table.Column<long>(type: "INTEGER", nullable: true),
                    IgnoreIfHashUnavailableId = table.Column<long>(type: "INTEGER", nullable: true),
                    IgnoreIfPackAvailableId = table.Column<long>(type: "INTEGER", nullable: true),
                    IgnoreIfPackUnavailableId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequiredMods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequiredMods_ModManifestHashes_IgnoreIfHashAvailableId",
                        column: x => x.IgnoreIfHashAvailableId,
                        principalTable: "ModManifestHashes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequiredMods_ModManifestHashes_IgnoreIfHashUnavailableId",
                        column: x => x.IgnoreIfHashUnavailableId,
                        principalTable: "ModManifestHashes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequiredMods_ModManifests_ModManfiestId",
                        column: x => x.ModManfiestId,
                        principalTable: "ModManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequiredMods_PackCodes_IgnoreIfPackAvailableId",
                        column: x => x.IgnoreIfPackAvailableId,
                        principalTable: "PackCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequiredMods_PackCodes_IgnoreIfPackUnavailableId",
                        column: x => x.IgnoreIfPackUnavailableId,
                        principalTable: "PackCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequiredMods_RequirementIdentifiers_RequirementIdentifierId",
                        column: x => x.RequirementIdentifierId,
                        principalTable: "RequirementIdentifiers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ModManifestHashRequiredMod",
                columns: table => new
                {
                    DependentsId = table.Column<long>(type: "INTEGER", nullable: false),
                    HashesId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModManifestHashRequiredMod", x => new { x.DependentsId, x.HashesId });
                    table.ForeignKey(
                        name: "FK_ModManifestHashRequiredMod_ModManifestHashes_HashesId",
                        column: x => x.HashesId,
                        principalTable: "ModManifestHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModManifestHashRequiredMod_RequiredMods_DependentsId",
                        column: x => x.DependentsId,
                        principalTable: "RequiredMods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FilesOfInterest_Path",
                table: "FilesOfInterest",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HashResourceKeys_ModManifestId",
                table: "HashResourceKeys",
                column: "ModManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_IntentionalOverrides_ModManfiestId",
                table: "IntentionalOverrides",
                column: "ModManfiestId");

            migrationBuilder.CreateIndex(
                name: "IX_IntentionalOverrides_ModManifestHashId",
                table: "IntentionalOverrides",
                column: "ModManifestHashId");

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
                name: "IX_ModExclusivities_Name",
                table: "ModExclusivities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModExclusivityModManifest_SpecifiedByModManifestsId",
                table: "ModExclusivityModManifest",
                column: "SpecifiedByModManifestsId");

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
                name: "IX_ModManifestHashes_ModManifestId",
                table: "ModManifestHashes",
                column: "ModManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_ModManifestHashRequiredMod_HashesId",
                table: "ModManifestHashRequiredMod",
                column: "HashesId");

            migrationBuilder.CreateIndex(
                name: "IX_ModManifestPackCode_RequiredPacksId",
                table: "ModManifestPackCode",
                column: "RequiredPacksId");

            migrationBuilder.CreateIndex(
                name: "IX_ModManifests_CalculatedModManifestHashId",
                table: "ModManifests",
                column: "CalculatedModManifestHashId");

            migrationBuilder.CreateIndex(
                name: "IX_ModManifests_InscribedModManifestHashId",
                table: "ModManifests",
                column: "InscribedModManifestHashId");

            migrationBuilder.CreateIndex(
                name: "IX_ModManifests_ModFileHashId",
                table: "ModManifests",
                column: "ModFileHashId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackCodes_Code",
                table: "PackCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_IgnoreIfHashAvailableId",
                table: "RequiredMods",
                column: "IgnoreIfHashAvailableId");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_IgnoreIfHashUnavailableId",
                table: "RequiredMods",
                column: "IgnoreIfHashUnavailableId");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_IgnoreIfPackAvailableId",
                table: "RequiredMods",
                column: "IgnoreIfPackAvailableId");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_IgnoreIfPackUnavailableId",
                table: "RequiredMods",
                column: "IgnoreIfPackUnavailableId");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_ModManfiestId",
                table: "RequiredMods",
                column: "ModManfiestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequiredMods_RequirementIdentifierId",
                table: "RequiredMods",
                column: "RequirementIdentifierId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementIdentifiers_Identifier",
                table: "RequirementIdentifiers",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScriptModArchiveEntries_ModFileHashId_FullName",
                table: "ScriptModArchiveEntries",
                columns: new[] { "ModFileHashId", "FullName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_HashResourceKeys_ModManifests_ModManifestId",
                table: "HashResourceKeys",
                column: "ModManifestId",
                principalTable: "ModManifests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_IntentionalOverrides_ModManifestHashes_ModManifestHashId",
                table: "IntentionalOverrides",
                column: "ModManifestHashId",
                principalTable: "ModManifestHashes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_IntentionalOverrides_ModManifests_ModManfiestId",
                table: "IntentionalOverrides",
                column: "ModManfiestId",
                principalTable: "ModManifests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModCreatorModManifest_ModManifests_AttributedModsId",
                table: "ModCreatorModManifest",
                column: "AttributedModsId",
                principalTable: "ModManifests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModCreatorRequiredMod_RequiredMods_AttributedRequiredModsId",
                table: "ModCreatorRequiredMod",
                column: "AttributedRequiredModsId",
                principalTable: "RequiredMods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModExclusivityModManifest_ModManifests_SpecifiedByModManifestsId",
                table: "ModExclusivityModManifest",
                column: "SpecifiedByModManifestsId",
                principalTable: "ModManifests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModFeatureModManifest_ModManifests_SpecifiedByModManifestsId",
                table: "ModFeatureModManifest",
                column: "SpecifiedByModManifestsId",
                principalTable: "ModManifests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModFeatureRequiredMod_RequiredMods_SpecifiedByRequiredModsId",
                table: "ModFeatureRequiredMod",
                column: "SpecifiedByRequiredModsId",
                principalTable: "RequiredMods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModManifestHashes_ModManifests_ModManifestId",
                table: "ModManifestHashes",
                column: "ModManifestId",
                principalTable: "ModManifests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModManifestHashes_ModManifests_ModManifestId",
                table: "ModManifestHashes");

            migrationBuilder.DropTable(
                name: "FilesOfInterest");

            migrationBuilder.DropTable(
                name: "HashResourceKeys");

            migrationBuilder.DropTable(
                name: "IntentionalOverrides");

            migrationBuilder.DropTable(
                name: "ModCreatorModManifest");

            migrationBuilder.DropTable(
                name: "ModCreatorRequiredMod");

            migrationBuilder.DropTable(
                name: "ModExclusivityModManifest");

            migrationBuilder.DropTable(
                name: "ModFeatureModManifest");

            migrationBuilder.DropTable(
                name: "ModFeatureRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFileResourceTopologySnapshot");

            migrationBuilder.DropTable(
                name: "ModFiles");

            migrationBuilder.DropTable(
                name: "ModManifestHashRequiredMod");

            migrationBuilder.DropTable(
                name: "ModManifestPackCode");

            migrationBuilder.DropTable(
                name: "ScriptModArchiveEntries");

            migrationBuilder.DropTable(
                name: "ModCreators");

            migrationBuilder.DropTable(
                name: "ModExclusivities");

            migrationBuilder.DropTable(
                name: "ModFeatures");

            migrationBuilder.DropTable(
                name: "ModFileResources");

            migrationBuilder.DropTable(
                name: "TopologySnapshots");

            migrationBuilder.DropTable(
                name: "RequiredMods");

            migrationBuilder.DropTable(
                name: "PackCodes");

            migrationBuilder.DropTable(
                name: "RequirementIdentifiers");

            migrationBuilder.DropTable(
                name: "ModManifests");

            migrationBuilder.DropTable(
                name: "ModFileHashes");

            migrationBuilder.DropTable(
                name: "ModManifestHashes");
        }
    }
}
