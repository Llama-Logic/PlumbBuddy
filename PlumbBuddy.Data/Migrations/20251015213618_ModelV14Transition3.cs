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
    public partial class ModelV14Transition3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModFiles_Path_Creation_LastWrite_Size",
                table: "ModFiles");

            migrationBuilder.DropColumn(
                name: "Creation",
                table: "ModFiles");

            migrationBuilder.DropColumn(
                name: "LastWrite",
                table: "ModFiles");

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_Path_CreationSeconds_LastWriteSeconds_Size",
                table: "ModFiles",
                columns: new[] { "Path", "CreationSeconds", "LastWriteSeconds", "Size" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ModFiles_Path_CreationSeconds_LastWriteSeconds_Size",
                table: "ModFiles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Creation",
                table: "ModFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastWrite",
                table: "ModFiles",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_ModFiles_Path_Creation_LastWrite_Size",
                table: "ModFiles",
                columns: new[] { "Path", "Creation", "LastWrite", "Size" });
        }
    }
}
