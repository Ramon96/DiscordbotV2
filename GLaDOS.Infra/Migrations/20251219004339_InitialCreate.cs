using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordUsers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    discord_id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordUsers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeUsers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Username = table.Column<string>(type: "text", nullable: false),
                    DiscordUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeUsers", x => x.id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeUsers_DiscordUsers_DiscordUserId",
                        column: x => x.DiscordUserId,
                        principalTable: "DiscordUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeActivities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeActivities", x => x.id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeActivities_OldschoolRunescapeUsers_id",
                        column: x => x.id,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeStats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeStats", x => x.id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_id",
                        column: x => x.id,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiscordUsers_discord_id",
                table: "DiscordUsers",
                column: "discord_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeUsers_DiscordUserId",
                table: "OldschoolRunescapeUsers",
                column: "DiscordUserId");

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
                name: "OldschoolRunescapeActivities");

            migrationBuilder.DropTable(
                name: "OldschoolRunescapeStats");

            migrationBuilder.DropTable(
                name: "OldschoolRunescapeUsers");

            migrationBuilder.DropTable(
                name: "DiscordUsers");
        }
    }
}
