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
    public partial class ModelV16 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ModFiles",
                type: "TEXT",
                nullable: false,
                computedColumnSql: "CASE WHEN instr([Path], '\\') > 0 THEN substr([Path], instr([Path], '\\') + 1) WHEN instr([Path], '/') > 0 THEN substr([Path], instr([Path], '/') + 1) ELSE [Path] END",
                stored: false);

            migrationBuilder.AddColumn<string>(
                name: "FolderPath",
                table: "ModFiles",
                type: "TEXT",
                nullable: false,
                computedColumnSql: "CASE WHEN instr([Path], '\\') > 0 THEN substr([Path], 1, instr([Path], '\\') - 1) WHEN instr([Path], '/') > 0 THEN substr([Path], 1, instr([Path], '/') - 1) ELSE '' END",
                stored: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ModFiles");

            migrationBuilder.DropColumn(
                name: "FolderPath",
                table: "ModFiles");
        }
    }
}
