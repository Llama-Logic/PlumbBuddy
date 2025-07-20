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
    public partial class ModelV9 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StringTableLocalePrefix",
                table: "ModFileResources");

            migrationBuilder.DropColumn(
                name: "StringTableLocalePrefix",
                table: "GameResourcePackageResources");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "StringTableLocalePrefix",
                table: "ModFileResources",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<byte>(
                name: "StringTableLocalePrefix",
                table: "GameResourcePackageResources",
                type: "INTEGER",
                nullable: false,
                defaultValue: (byte)0);
        }
    }
}
