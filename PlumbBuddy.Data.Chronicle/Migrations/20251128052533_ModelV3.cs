using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable IDE0079
#pragma warning disable CA1062
#pragma warning disable CA1861
#pragma warning disable IDE0053
#pragma warning disable IDE0161
#pragma warning disable IDE0300

namespace PlumbBuddy.Data.Chronicle.Migrations
{
    /// <inheritdoc />
    public partial class ModelV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DisabledSavePackageSnapshotDefectTypes",
                columns: table => new
                {
                    SavePackageSnapshotDefectType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisabledSavePackageSnapshotDefectTypes", x => x.SavePackageSnapshotDefectType);
                });

            migrationBuilder.CreateTable(
                name: "SavePackageSnapshotDefects",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SavePackageSnapshotId = table.Column<long>(type: "INTEGER", nullable: false),
                    SavePackageSnapshotDefectType = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavePackageSnapshotDefects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavePackageSnapshotDefects_SavePackageSnapshots_SavePackageSnapshotId",
                        column: x => x.SavePackageSnapshotId,
                        principalTable: "SavePackageSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavePackageSnapshotDefects_SavePackageSnapshotId",
                table: "SavePackageSnapshotDefects",
                column: "SavePackageSnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DisabledSavePackageSnapshotDefectTypes");

            migrationBuilder.DropTable(
                name: "SavePackageSnapshotDefects");
        }
    }
}
