using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hikaria.Core.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class add_livelobby : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiveLobbies",
                columns: table => new
                {
                    LobbyID = table.Column<long>(type: "bigint", nullable: false),
                    LobbyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrivacySettings_Privacy = table.Column<int>(type: "int", nullable: false),
                    PrivacySettings_HasPassword = table.Column<bool>(type: "bit", nullable: false),
                    DetailedInfo_HostSteamID = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    DetailedInfo_Expedition = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetailedInfo_ExpeditionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetailedInfo_OpenSlots = table.Column<int>(type: "int", nullable: false),
                    DetailedInfo_MaxPlayerSlots = table.Column<int>(type: "int", nullable: false),
                    DetailedInfo_RegionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DetailedInfo_Revision = table.Column<int>(type: "int", nullable: false),
                    DetailedInfo_IsPlayingModded = table.Column<bool>(type: "bit", nullable: false),
                    DetailedInfo_SteamIDsInLobby = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StatusInfo_StatusInfo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpirationTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveLobbies", x => x.LobbyID);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveLobbies");
        }
    }
}
