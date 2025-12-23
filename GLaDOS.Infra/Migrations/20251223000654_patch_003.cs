using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class patch_003 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "discord_id",
                table: "DiscordUsers",
                newName: "DiscordId");

            migrationBuilder.RenameIndex(
                name: "IX_DiscordUsers_discord_id",
                table: "DiscordUsers",
                newName: "IX_DiscordUsers_DiscordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiscordId",
                table: "DiscordUsers",
                newName: "discord_id");

            migrationBuilder.RenameIndex(
                name: "IX_DiscordUsers_DiscordId",
                table: "DiscordUsers",
                newName: "IX_DiscordUsers_discord_id");
        }
    }
}
