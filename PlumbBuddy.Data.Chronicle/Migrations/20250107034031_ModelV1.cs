using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumbBuddy.Data.Chronicle.Migrations
{
    /// <inheritdoc />
    public partial class ModelV1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChroniclePropertySets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NucleusId = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 8, nullable: false),
                    Created = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 8, nullable: false),
                    BasisNucleusId = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 8, nullable: true),
                    BasisCreated = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 8, nullable: true),
                    BasisOriginalPackageSha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: true),
                    GameNameOverride = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Thumbnail = table.Column<byte[]>(type: "BLOB", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChroniclePropertySets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnownSavePackageHashes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnownSavePackageHashes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavePackageResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 16, nullable: false),
                    CompressionType = table.Column<byte>(type: "INTEGER", nullable: false),
                    ContentZLib = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentSize = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavePackageResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SnapshotModFiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    LastWriteTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SnapshotModFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavePackageSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OriginalSavePackageHashId = table.Column<long>(type: "INTEGER", nullable: true),
                    EnhancedSavePackageHashId = table.Column<long>(type: "INTEGER", nullable: true),
                    LastWriteTime = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Thumbnail = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ActiveHouseholdName = table.Column<string>(type: "TEXT", nullable: true),
                    LastPlayedLotName = table.Column<string>(type: "TEXT", nullable: true),
                    LastPlayedWorldName = table.Column<string>(type: "TEXT", nullable: true),
                    WasLive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavePackageSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavePackageSnapshots_KnownSavePackageHashes_EnhancedSavePackageHashId",
                        column: x => x.EnhancedSavePackageHashId,
                        principalTable: "KnownSavePackageHashes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SavePackageSnapshots_KnownSavePackageHashes_OriginalSavePackageHashId",
                        column: x => x.OriginalSavePackageHashId,
                        principalTable: "KnownSavePackageHashes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceSnapshotDeltas",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SavePackageSnapshotId = table.Column<long>(type: "INTEGER", nullable: false),
                    SavePackageResourceId = table.Column<long>(type: "INTEGER", nullable: false),
                    PatchZLib = table.Column<byte[]>(type: "BLOB", nullable: false),
                    PatchSize = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceSnapshotDeltas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceSnapshotDeltas_SavePackageResources_SavePackageResourceId",
                        column: x => x.SavePackageResourceId,
                        principalTable: "SavePackageResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceSnapshotDeltas_SavePackageSnapshots_SavePackageSnapshotId",
                        column: x => x.SavePackageSnapshotId,
                        principalTable: "SavePackageSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavePackageResourceSavePackageSnapshot",
                columns: table => new
                {
                    ResourcesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SnapshotsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavePackageResourceSavePackageSnapshot", x => new { x.ResourcesId, x.SnapshotsId });
                    table.ForeignKey(
                        name: "FK_SavePackageResourceSavePackageSnapshot_SavePackageResources_ResourcesId",
                        column: x => x.ResourcesId,
                        principalTable: "SavePackageResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavePackageResourceSavePackageSnapshot_SavePackageSnapshots_SnapshotsId",
                        column: x => x.SnapshotsId,
                        principalTable: "SavePackageSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavePackageSnapshotSnapshotModFile",
                columns: table => new
                {
                    ModFilesId = table.Column<long>(type: "INTEGER", nullable: false),
                    SnapshotsId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavePackageSnapshotSnapshotModFile", x => new { x.ModFilesId, x.SnapshotsId });
                    table.ForeignKey(
                        name: "FK_SavePackageSnapshotSnapshotModFile_SavePackageSnapshots_SnapshotsId",
                        column: x => x.SnapshotsId,
                        principalTable: "SavePackageSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SavePackageSnapshotSnapshotModFile_SnapshotModFiles_ModFilesId",
                        column: x => x.ModFilesId,
                        principalTable: "SnapshotModFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnownSavePackageHashes_Sha256",
                table: "KnownSavePackageHashes",
                column: "Sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSnapshotDeltas_SavePackageResourceId",
                table: "ResourceSnapshotDeltas",
                column: "SavePackageResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSnapshotDeltas_SavePackageSnapshotId",
                table: "ResourceSnapshotDeltas",
                column: "SavePackageSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_SavePackageResourceSavePackageSnapshot_SnapshotsId",
                table: "SavePackageResourceSavePackageSnapshot",
                column: "SnapshotsId");

            migrationBuilder.CreateIndex(
                name: "IX_SavePackageSnapshots_EnhancedSavePackageHashId",
                table: "SavePackageSnapshots",
                column: "EnhancedSavePackageHashId");

            migrationBuilder.CreateIndex(
                name: "IX_SavePackageSnapshots_OriginalSavePackageHashId",
                table: "SavePackageSnapshots",
                column: "OriginalSavePackageHashId");

            migrationBuilder.CreateIndex(
                name: "IX_SavePackageSnapshotSnapshotModFile_SnapshotsId",
                table: "SavePackageSnapshotSnapshotModFile",
                column: "SnapshotsId");

            migrationBuilder.CreateIndex(
                name: "IX_SnapshotModFiles_Path_LastWriteTime_Size_Sha256",
                table: "SnapshotModFiles",
                columns: new[] { "Path", "LastWriteTime", "Size", "Sha256" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChroniclePropertySets");

            migrationBuilder.DropTable(
                name: "ResourceSnapshotDeltas");

            migrationBuilder.DropTable(
                name: "SavePackageResourceSavePackageSnapshot");

            migrationBuilder.DropTable(
                name: "SavePackageSnapshotSnapshotModFile");

            migrationBuilder.DropTable(
                name: "SavePackageResources");

            migrationBuilder.DropTable(
                name: "SavePackageSnapshots");

            migrationBuilder.DropTable(
                name: "SnapshotModFiles");

            migrationBuilder.DropTable(
                name: "KnownSavePackageHashes");
        }
    }
}
