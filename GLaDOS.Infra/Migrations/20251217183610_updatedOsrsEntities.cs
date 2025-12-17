using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class updatedOsrsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeBosses_OldschoolRunescapeUsers_Id",
                table: "OldschoolRunescapeBosses");

            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_Id",
                table: "OldschoolRunescapeStats");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeUsers_Username",
                table: "OldschoolRunescapeUsers");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeBosses_Id",
                table: "OldschoolRunescapeBosses");

            migrationBuilder.DropIndex(
                name: "IX_DiscordUsers_DiscordId",
                table: "DiscordUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OldschoolRunescapeStats",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OldschoolRunescapeStats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OldschoolRunescapeBosses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "OldschoolRunescapeBosses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeStats_UserId",
                table: "OldschoolRunescapeStats",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeBosses_UserId",
                table: "OldschoolRunescapeBosses",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeBosses_OldschoolRunescapeUsers_UserId",
                table: "OldschoolRunescapeBosses",
                column: "UserId",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_UserId",
                table: "OldschoolRunescapeStats",
                column: "UserId",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeBosses_OldschoolRunescapeUsers_UserId",
                table: "OldschoolRunescapeBosses");

            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_UserId",
                table: "OldschoolRunescapeStats");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeStats_UserId",
                table: "OldschoolRunescapeStats");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeBosses_UserId",
                table: "OldschoolRunescapeBosses");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OldschoolRunescapeStats");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "OldschoolRunescapeBosses");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OldschoolRunescapeStats",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OldschoolRunescapeBosses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeUsers_Username",
                table: "OldschoolRunescapeUsers",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeBosses_Id",
                table: "OldschoolRunescapeBosses",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUsers_DiscordId",
                table: "DiscordUsers",
                column: "DiscordId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeBosses_OldschoolRunescapeUsers_Id",
                table: "OldschoolRunescapeBosses",
                column: "Id",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_Id",
                table: "OldschoolRunescapeStats",
                column: "Id",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
