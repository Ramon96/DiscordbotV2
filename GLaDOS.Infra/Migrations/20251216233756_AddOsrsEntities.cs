using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOsrsEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeBosses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    BossId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Rank = table.Column<long>(type: "bigint", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeBosses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeBosses_OldschoolRunescapeUsers_Id",
                        column: x => x.Id,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeStats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Rank = table.Column<long>(type: "bigint", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_Id",
                        column: x => x.Id,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUsers_DiscordId",
                table: "DiscordUsers",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeBosses_Id",
                table: "OldschoolRunescapeBosses",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeUsers_Username",
                table: "OldschoolRunescapeUsers",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OldschoolRunescapeBosses");

            migrationBuilder.DropTable(
                name: "OldschoolRunescapeStats");

            migrationBuilder.DropTable(
                name: "OldschoolRunescapeUsers");

            migrationBuilder.DropIndex(
                name: "IX_DiscordUsers_DiscordId",
                table: "DiscordUsers");
        }
    }
}
