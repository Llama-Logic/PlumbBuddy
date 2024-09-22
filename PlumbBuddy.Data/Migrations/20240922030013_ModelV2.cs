using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumbBuddy.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModFeatureModManifest");

            migrationBuilder.DropTable(
                name: "ModFeatureRequiredMod");

            migrationBuilder.DropTable(
                name: "ModFeatures");
        }
    }
}
