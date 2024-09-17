using System;
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
                name: "IX_ModFileResourceTopologySnapshot_TopologySnapshotsId",
                table: "ModFileResourceTopologySnapshot",
                column: "TopologySnapshotsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModFileResourceTopologySnapshot");

            migrationBuilder.DropTable(
                name: "TopologySnapshots");
        }
    }
}
