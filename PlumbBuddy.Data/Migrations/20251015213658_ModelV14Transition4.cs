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
    public partial class ModelV14Transition4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastWriteSeconds",
                table: "ModFiles",
                newName: "LastWrite");

            migrationBuilder.RenameColumn(
                name: "CreationSeconds",
                table: "ModFiles",
                newName: "Creation");

            migrationBuilder.RenameIndex(
                name: "IX_ModFiles_Path_CreationSeconds_LastWriteSeconds_Size",
                table: "ModFiles",
                newName: "IX_ModFiles_Path_Creation_LastWrite_Size");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastWrite",
                table: "ModFiles",
                newName: "LastWriteSeconds");

            migrationBuilder.RenameColumn(
                name: "Creation",
                table: "ModFiles",
                newName: "CreationSeconds");

            migrationBuilder.RenameIndex(
                name: "IX_ModFiles_Path_Creation_LastWrite_Size",
                table: "ModFiles",
                newName: "IX_ModFiles_Path_CreationSeconds_LastWriteSeconds_Size");
        }
    }
}
