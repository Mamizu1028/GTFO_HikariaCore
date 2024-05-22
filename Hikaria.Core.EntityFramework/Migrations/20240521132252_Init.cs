using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hikaria.Core.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BannedPlayers",
                columns: table => new
                {
                    SteamID = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DateBanned = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BannedPlayers", x => x.SteamID);
                });

            migrationBuilder.CreateTable(
                name: "SteamUsers",
                columns: table => new
                {
                    SteamID = table.Column<long>(type: "bigint", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: ""),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Privilege = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "None")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SteamUsers", x => x.SteamID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BannedPlayers");

            migrationBuilder.DropTable(
                name: "SteamUsers");
        }
    }
}
