using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlumbBuddy.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModelV4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Size",
                table: "ModFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastWrite",
                table: "ModFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Creation",
                table: "ModFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ModHoundReports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestSha256 = table.Column<byte[]>(type: "BLOB", fixedLength: true, maxLength: 32, nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ResultId = table.Column<string>(type: "TEXT", nullable: false),
                    ReportHtml = table.Column<string>(type: "TEXT", nullable: false),
                    Retrieved = table.Column<long>(type: "INTEGER", nullable: false),
                    LastEditedAtAny = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModHoundReportIncompatibilityRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModHoundReportId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReportIncompatibilityRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModHoundReportIncompatibilityRecords_ModHoundReports_ModHoundReportId",
                        column: x => x.ModHoundReportId,
                        principalTable: "ModHoundReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModHoundReportMissingRequirementsRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModHoundReportId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReportMissingRequirementsRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModHoundReportMissingRequirementsRecords_ModHoundReports_ModHoundReportId",
                        column: x => x.ModHoundReportId,
                        principalTable: "ModHoundReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModHoundReportNotTrackedRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModHoundReportId = table.Column<long>(type: "INTEGER", nullable: false),
                    FileDate = table.Column<long>(type: "INTEGER", nullable: false),
                    FileDateString = table.Column<string>(type: "TEXT", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FileType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReportNotTrackedRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModHoundReportNotTrackedRecords_ModHoundReports_ModHoundReportId",
                        column: x => x.ModHoundReportId,
                        principalTable: "ModHoundReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModHoundReportRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModHoundReportId = table.Column<long>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    ModName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatorName = table.Column<string>(type: "TEXT", nullable: false),
                    LastUpdateDate = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUpdateDateString = table.Column<string>(type: "TEXT", nullable: true),
                    DateOfInstalledFile = table.Column<long>(type: "INTEGER", nullable: false),
                    DateOfInstalledFileString = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ModLinkOrIndexText = table.Column<string>(type: "TEXT", nullable: true),
                    ModLinkOrIndexHref = table.Column<string>(type: "TEXT", nullable: true),
                    UpdateNotes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReportRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModHoundReportRecords_ModHoundReports_ModHoundReportId",
                        column: x => x.ModHoundReportId,
                        principalTable: "ModHoundReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModHoundReportIncompatibilityRecordParts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModHoundReportIncompatibilityRecordId = table.Column<long>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReportIncompatibilityRecordParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModHoundReportIncompatibilityRecordParts_ModHoundReportIncompatibilityRecords_ModHoundReportIncompatibilityRecordId",
                        column: x => x.ModHoundReportIncompatibilityRecordId,
                        principalTable: "ModHoundReportIncompatibilityRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModHoundReportMissingRequirementsRecordDependencies",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModHoundReportMissingRequirementsRecordId = table.Column<long>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false),
                    ModLinkOrIndexText = table.Column<string>(type: "TEXT", nullable: true),
                    ModLinkOrIndexHref = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReportMissingRequirementsRecordDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModHoundReportMissingRequirementsRecordDependencies_ModHoundReportMissingRequirementsRecords_ModHoundReportMissingRequirementsRecordId",
                        column: x => x.ModHoundReportMissingRequirementsRecordId,
                        principalTable: "ModHoundReportMissingRequirementsRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ModHoundReportMissingRequirementsRecordDependents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ModHoundReportMissingRequirementsRecordId = table.Column<long>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModHoundReportMissingRequirementsRecordDependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModHoundReportMissingRequirementsRecordDependents_ModHoundReportMissingRequirementsRecords_ModHoundReportMissingRequirementsRecordId",
                        column: x => x.ModHoundReportMissingRequirementsRecordId,
                        principalTable: "ModHoundReportMissingRequirementsRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReportIncompatibilityRecordParts_ModHoundReportIncompatibilityRecordId",
                table: "ModHoundReportIncompatibilityRecordParts",
                column: "ModHoundReportIncompatibilityRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReportIncompatibilityRecords_ModHoundReportId",
                table: "ModHoundReportIncompatibilityRecords",
                column: "ModHoundReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReportMissingRequirementsRecordDependencies_ModHoundReportMissingRequirementsRecordId",
                table: "ModHoundReportMissingRequirementsRecordDependencies",
                column: "ModHoundReportMissingRequirementsRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReportMissingRequirementsRecordDependents_ModHoundReportMissingRequirementsRecordId",
                table: "ModHoundReportMissingRequirementsRecordDependents",
                column: "ModHoundReportMissingRequirementsRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReportMissingRequirementsRecords_ModHoundReportId",
                table: "ModHoundReportMissingRequirementsRecords",
                column: "ModHoundReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReportNotTrackedRecords_ModHoundReportId",
                table: "ModHoundReportNotTrackedRecords",
                column: "ModHoundReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReportRecords_ModHoundReportId",
                table: "ModHoundReportRecords",
                column: "ModHoundReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ModHoundReports_RequestSha256_Retrieved",
                table: "ModHoundReports",
                columns: new[] { "RequestSha256", "Retrieved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModHoundReportIncompatibilityRecordParts");

            migrationBuilder.DropTable(
                name: "ModHoundReportMissingRequirementsRecordDependencies");

            migrationBuilder.DropTable(
                name: "ModHoundReportMissingRequirementsRecordDependents");

            migrationBuilder.DropTable(
                name: "ModHoundReportNotTrackedRecords");

            migrationBuilder.DropTable(
                name: "ModHoundReportRecords");

            migrationBuilder.DropTable(
                name: "ModHoundReportIncompatibilityRecords");

            migrationBuilder.DropTable(
                name: "ModHoundReportMissingRequirementsRecords");

            migrationBuilder.DropTable(
                name: "ModHoundReports");

            migrationBuilder.AlterColumn<long>(
                name: "Size",
                table: "ModFiles",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastWrite",
                table: "ModFiles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Creation",
                table: "ModFiles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");
        }
    }
}
