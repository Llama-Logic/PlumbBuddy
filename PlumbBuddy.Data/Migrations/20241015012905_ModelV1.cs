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
                name: "ElectronicArtsPromoCodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicArtsPromoCodes", x => x.Id);
                });

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
                    ModFileManifestId = table.Column<long>(type: "INTEGER", nullable: false),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: false),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HashResourceKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModCreatorModFileManifest",
                columns: table => new
                {
                    AttributedModsId = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatorsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModCreatorModFileManifest", x => new { x.AttributedModsId, x.CreatorsId });
                    table.ForeignKey(
                        name: "FK_ModCreatorModFileManifest_ModCreators_CreatorsId",
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
                name: "ModExclusivityModFileManifest",
                columns: table => new
                {
                    ExclusivitiesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SpecifiedByModFileManifestsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModExclusivityModFileManifest", x => new { x.ExclusivitiesId, x.SpecifiedByModFileManifestsId });
                    table.ForeignKey(
                        name: "FK_ModExclusivityModFileManifest_ModExclusivities_ExclusivitiesId",
                        column: x => x.ExclusivitiesId,
                        principalTable: "ModExclusivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFeatureModFileManifest",
                columns: table => new
                {
                    FeaturesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SpecifiedByModFileManifestsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFeatureModFileManifest", x => new { x.FeaturesId, x.SpecifiedByModFileManifestsId });
                    table.ForeignKey(
                        name: "FK_ModFeatureModFileManifest_ModFeatures_FeaturesId",
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
                name: "ModFileManifestHashes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    ModFileManifestId = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileManifestHashes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModFileManifests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModFileHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    InscribedModFileManifestHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    ResourceHashStrategy = table.Column<int>(type: "INTEGER", nullable: true),
                    CalculatedModFileManifestHashId = table.Column<long>(type: "INTEGER", nullable: false),
                    ElectronicArtsPromoCodeId = table.Column<long>(type: "INTEGER", nullable: true),
                    TuningName = table.Column<string>(type: "TEXT", nullable: true),
                    TuningFullInstance = table.Column<long>(type: "INTEGER", nullable: true),
                    KeyType = table.Column<int>(type: "INTEGER", nullable: true),
                    KeyGroup = table.Column<int>(type: "INTEGER", nullable: true),
                    KeyFullInstance = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileManifests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModFileManifests_ElectronicArtsPromoCodes_ElectronicArtsPromoCodeId",
                        column: x => x.ElectronicArtsPromoCodeId,
                        principalTable: "ElectronicArtsPromoCodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ModFileManifests_ModFileHashes_ModFileHashId",
                        column: x => x.ModFileHashId,
                        principalTable: "ModFileHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileManifests_ModFileManifestHashes_CalculatedModFileManifestHashId",
                        column: x => x.CalculatedModFileManifestHashId,
                        principalTable: "ModFileManifestHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileManifests_ModFileManifestHashes_InscribedModFileManifestHashId",
                        column: x => x.InscribedModFileManifestHashId,
                        principalTable: "ModFileManifestHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFileManifestPackCode",
                columns: table => new
                {
                    IncompatiblePacksId = table.Column<long>(type: "INTEGER", nullable: false),
                    IncompatibleWithModsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileManifestPackCode", x => new { x.IncompatiblePacksId, x.IncompatibleWithModsId });
                    table.ForeignKey(
                        name: "FK_ModFileManifestPackCode_ModFileManifests_IncompatibleWithModsId",
                        column: x => x.IncompatibleWithModsId,
                        principalTable: "ModFileManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileManifestPackCode_PackCodes_IncompatiblePacksId",
                        column: x => x.IncompatiblePacksId,
                        principalTable: "PackCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModFileManifestPackCode1",
                columns: table => new
                {
                    RequiredByModsId = table.Column<long>(type: "INTEGER", nullable: false),
                    RequiredPacksId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileManifestPackCode1", x => new { x.RequiredByModsId, x.RequiredPacksId });
                    table.ForeignKey(
                        name: "FK_ModFileManifestPackCode1_ModFileManifests_RequiredByModsId",
                        column: x => x.RequiredByModsId,
                        principalTable: "ModFileManifests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileManifestPackCode1_PackCodes_RequiredPacksId",
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
                        name: "FK_RequiredMods_ModFileManifestHashes_IgnoreIfHashAvailableId",
                        column: x => x.IgnoreIfHashAvailableId,
                        principalTable: "ModFileManifestHashes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequiredMods_ModFileManifestHashes_IgnoreIfHashUnavailableId",
                        column: x => x.IgnoreIfHashUnavailableId,
                        principalTable: "ModFileManifestHashes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequiredMods_ModFileManifests_ModManfiestId",
                        column: x => x.ModManfiestId,
                        principalTable: "ModFileManifests",
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
                name: "ModFileManifestHashRequiredMod",
                columns: table => new
                {
                    DependentsId = table.Column<long>(type: "INTEGER", nullable: false),
                    HashesId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModFileManifestHashRequiredMod", x => new { x.DependentsId, x.HashesId });
                    table.ForeignKey(
                        name: "FK_ModFileManifestHashRequiredMod_ModFileManifestHashes_HashesId",
                        column: x => x.HashesId,
                        principalTable: "ModFileManifestHashes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModFileManifestHashRequiredMod_RequiredMods_DependentsId",
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
                name: "IX_HashResourceKeys_ModFileManifestId",
                table: "HashResourceKeys",
                column: "ModFileManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_ModCreatorModFileManifest_CreatorsId",
                table: "ModCreatorModFileManifest",
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
                name: "IX_ModExclusivityModFileManifest_SpecifiedByModFileManifestsId",
                table: "ModExclusivityModFileManifest",
                column: "SpecifiedByModFileManifestsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFeatureModFileManifest_SpecifiedByModFileManifestsId",
                table: "ModFeatureModFileManifest",
                column: "SpecifiedByModFileManifestsId");

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
                name: "IX_ModFileManifestHashes_ModFileManifestId",
                table: "ModFileManifestHashes",
                column: "ModFileManifestId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileManifestHashRequiredMod_HashesId",
                table: "ModFileManifestHashRequiredMod",
                column: "HashesId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileManifestPackCode_IncompatibleWithModsId",
                table: "ModFileManifestPackCode",
                column: "IncompatibleWithModsId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileManifestPackCode1_RequiredPacksId",
                table: "ModFileManifestPackCode1",
                column: "RequiredPacksId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileManifests_CalculatedModFileManifestHashId",
                table: "ModFileManifests",
                column: "CalculatedModFileManifestHashId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileManifests_ElectronicArtsPromoCodeId",
                table: "ModFileManifests",
                column: "ElectronicArtsPromoCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileManifests_InscribedModFileManifestHashId",
                table: "ModFileManifests",
                column: "InscribedModFileManifestHashId");

            migrationBuilder.CreateIndex(
                name: "IX_ModFileManifests_ModFileHashId",
                table: "ModFileManifests",
                column: "ModFileHashId",
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
                name: "FK_HashResourceKeys_ModFileManifests_ModFileManifestId",
                table: "HashResourceKeys",
                column: "ModFileManifestId",
                principalTable: "ModFileManifests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModCreatorModFileManifest_ModFileManifests_AttributedModsId",
                table: "ModCreatorModFileManifest",
                column: "AttributedModsId",
                principalTable: "ModFileManifests",
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
                name: "FK_ModExclusivityModFileManifest_ModFileManifests_SpecifiedByModFileManifestsId",
                table: "ModExclusivityModFileManifest",
                column: "SpecifiedByModFileManifestsId",
                principalTable: "ModFileManifests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ModFeatureModFileManifest_ModFileManifests_SpecifiedByModFileManifestsId",
                table: "ModFeatureModFileManifest",
                column: "SpecifiedByModFileManifestsId",
                principalTable: "ModFileManifests",
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
                name: "FK_ModFileManifestHashes_ModFileManifests_ModFileManifestId",
                table: "ModFileManifestHashes",
                column: "ModFileManifestId",
                principalTable: "ModFileManifests",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ModFileManifestHashes_ModFileManifests_ModFileManifestId",
                table: "ModFileManifestHashes");

            migrationBuilder.DropTable(
                name: "FilesOfInterest");

            migrationBuilder.DropTable(
                name: "HashResourceKeys");

            migrationBuilder.DropTable(
                name: "ModCreatorModFileManifest");

            migrationBuilder.DropTable(
                name: "ModCreatorRequiredMod");

            migrationBuilder.DropTable(
                name: "ModExclusivityModFileManifest");

            migrationBuilder.DropTable(
                name: "ModFeatureModFileManifest");

            migrationBuilder.DropTable(
                name: "ModFeatureRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFileManifestHashRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFileManifestPackCode");

            migrationBuilder.DropTable(
                name: "ModFileManifestPackCode1");

            migrationBuilder.DropTable(
                name: "ModFileResourceTopologySnapshot");

            migrationBuilder.DropTable(
                name: "ModFiles");

            migrationBuilder.DropTable(
                name: "ScriptModArchiveEntries");

            migrationBuilder.DropTable(
                name: "ModCreators");

            migrationBuilder.DropTable(
                name: "ModExclusivities");

            migrationBuilder.DropTable(
                name: "ModFeatures");

            migrationBuilder.DropTable(
                name: "RequiredMods");

            migrationBuilder.DropTable(
                name: "ModFileResources");

            migrationBuilder.DropTable(
                name: "TopologySnapshots");

            migrationBuilder.DropTable(
                name: "PackCodes");

            migrationBuilder.DropTable(
                name: "RequirementIdentifiers");

            migrationBuilder.DropTable(
                name: "ModFileManifests");

            migrationBuilder.DropTable(
                name: "ElectronicArtsPromoCodes");

            migrationBuilder.DropTable(
                name: "ModFileHashes");

            migrationBuilder.DropTable(
                name: "ModFileManifestHashes");
        }
    }
}
