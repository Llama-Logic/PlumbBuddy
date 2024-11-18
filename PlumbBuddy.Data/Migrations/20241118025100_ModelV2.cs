using System;
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
    public partial class ModelV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsenceNoticed",
                table: "ModFiles");

            migrationBuilder.Sql("DELETE FROM ModFiles WHERE Path IS NULL");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "ModFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorrupt",
                table: "ModFileHashes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCorrupt",
                table: "ModFileHashes");

            migrationBuilder.AlterColumn<string>(
                name: "Path",
                table: "ModFiles",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AbsenceNoticed",
                table: "ModFiles",
                type: "TEXT",
                nullable: true);
        }
    }
}
