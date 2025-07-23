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
    public partial class ModelV10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sha256",
                table: "GameResourcePackages");

            migrationBuilder.CreateIndex(
                name: "IX_GameStringTableEntries_GameResourcePackageResourceId",
                table: "GameStringTableEntries",
                column: "GameResourcePackageResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_GameResourcePackageResources_GameResourcePackageId",
                table: "GameResourcePackageResources",
                column: "GameResourcePackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameStringTableEntries_GameResourcePackageResourceId",
                table: "GameStringTableEntries");

            migrationBuilder.DropIndex(
                name: "IX_GameResourcePackageResources_GameResourcePackageId",
                table: "GameResourcePackageResources");

            migrationBuilder.AddColumn<byte[]>(
                name: "Sha256",
                table: "GameResourcePackages",
                type: "BLOB",
                fixedLength: true,
                maxLength: 32,
                nullable: false,
                defaultValue: new byte[0]);
        }
    }
}
