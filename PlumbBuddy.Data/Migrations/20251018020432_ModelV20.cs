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
    public partial class ModelV20 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnDisk",
                table: "ModFiles");

            migrationBuilder.AddColumn<long>(
                name: "FoundAbsent",
                table: "ModFiles",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FoundAbsent",
                table: "ModFiles");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnDisk",
                table: "ModFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
