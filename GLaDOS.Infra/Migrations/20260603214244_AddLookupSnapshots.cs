using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeActivitySnapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "date", nullable: false),
                    ActivityId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeActivitySnapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeActivitySnapshots_OldschoolRunescapeUsers~",
                        column: x => x.OldschoolRunescapeUserId,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeLookups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    LookupDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeLookups", x => x.id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeLookups_OldschoolRunescapeUsers_Oldschool~",
                        column: x => x.OldschoolRunescapeUserId,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OldschoolRunescapeStatsSnapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotDate = table.Column<DateTime>(type: "date", nullable: false),
                    SkillId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Experience = table.Column<long>(type: "bigint", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OldschoolRunescapeStatsSnapshots", x => x.id);
                    table.ForeignKey(
                        name: "FK_OldschoolRunescapeStatsSnapshots_OldschoolRunescapeUsers_Ol~",
                        column: x => x.OldschoolRunescapeUserId,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeActivitySnapshots_id",
                table: "OldschoolRunescapeActivitySnapshots",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeActivitySnapshots_OldschoolRunescapeUserI~",
                table: "OldschoolRunescapeActivitySnapshots",
                columns: new[] { "OldschoolRunescapeUserId", "SnapshotDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeLookups_id",
                table: "OldschoolRunescapeLookups",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeLookups_OldschoolRunescapeUserId_DiscordU~",
                table: "OldschoolRunescapeLookups",
                columns: new[] { "OldschoolRunescapeUserId", "DiscordUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeStatsSnapshots_id",
                table: "OldschoolRunescapeStatsSnapshots",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeStatsSnapshots_OldschoolRunescapeUserId_S~",
                table: "OldschoolRunescapeStatsSnapshots",
                columns: new[] { "OldschoolRunescapeUserId", "SnapshotDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OldschoolRunescapeActivitySnapshots");

            migrationBuilder.DropTable(
                name: "OldschoolRunescapeLookups");

            migrationBuilder.DropTable(
                name: "OldschoolRunescapeStatsSnapshots");
        }
    }
}
