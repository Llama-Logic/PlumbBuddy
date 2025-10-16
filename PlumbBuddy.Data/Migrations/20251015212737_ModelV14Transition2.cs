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
    public partial class ModelV14Transition2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ModFiles SET CreationSeconds = strftime('%s', Creation), LastWriteSeconds = strftime('%s', LastWrite);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ModFiles SET Creation = strftime('%Y-%m-%d %H:%M:%S.0000000+00:00', CreationSeconds, 'unixepoch'), LastWrite = strftime(%Y-%m-%d %H:%M:%S.0000000+00:00', LastWriteSeconds, 'unixepoch');");
        }
    }
}
