using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class patch_004 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                table: "OldschoolRunescapeActivities",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "OsrsWikiCollectionLog",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemIds = table.Column<List<int>>(type: "integer[]", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsrsWikiCollectionLog", x => x.id);
                    table.ForeignKey(
                        name: "FK_OsrsWikiCollectionLog_OldschoolRunescapeUsers_OldschoolRune~",
                        column: x => x.OldschoolRunescapeUserId,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OsrsWikiDiary",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    easy_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    easy_tasks = table.Column<string>(type: "jsonb", nullable: false),
                    medium_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    medium_tasks = table.Column<string>(type: "jsonb", nullable: false),
                    hard_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hard_tasks = table.Column<string>(type: "jsonb", nullable: false),
                    elite_complete = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    elite_tasks = table.Column<string>(type: "jsonb", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsrsWikiDiary", x => x.id);
                    table.ForeignKey(
                        name: "FK_OsrsWikiDiary_OldschoolRunescapeUsers_OldschoolRunescapeUse~",
                        column: x => x.OldschoolRunescapeUserId,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OsrsWikiMusic",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Song = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsUnlocked = table.Column<bool>(type: "boolean", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsrsWikiMusic", x => x.id);
                    table.ForeignKey(
                        name: "FK_OsrsWikiMusic_OldschoolRunescapeUsers_OldschoolRunescapeUse~",
                        column: x => x.OldschoolRunescapeUserId,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OsrsWikiQuest",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OldschoolRunescapeUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsrsWikiQuest", x => x.id);
                    table.ForeignKey(
                        name: "FK_OsrsWikiQuest_OldschoolRunescapeUsers_OldschoolRunescapeUse~",
                        column: x => x.OldschoolRunescapeUserId,
                        principalTable: "OldschoolRunescapeUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeStats_id",
                table: "OldschoolRunescapeStats",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeActivities_id",
                table: "OldschoolRunescapeActivities",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiCollectionLog_id",
                table: "OsrsWikiCollectionLog",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiCollectionLog_OldschoolRunescapeUserId",
                table: "OsrsWikiCollectionLog",
                column: "OldschoolRunescapeUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiDiary_id",
                table: "OsrsWikiDiary",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiDiary_OldschoolRunescapeUserId",
                table: "OsrsWikiDiary",
                column: "OldschoolRunescapeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiMusic_id",
                table: "OsrsWikiMusic",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiMusic_OldschoolRunescapeUserId",
                table: "OsrsWikiMusic",
                column: "OldschoolRunescapeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiQuest_id",
                table: "OsrsWikiQuest",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsWikiQuest_OldschoolRunescapeUserId",
                table: "OsrsWikiQuest",
                column: "OldschoolRunescapeUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OsrsWikiCollectionLog");

            migrationBuilder.DropTable(
                name: "OsrsWikiDiary");

            migrationBuilder.DropTable(
                name: "OsrsWikiMusic");

            migrationBuilder.DropTable(
                name: "OsrsWikiQuest");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeStats_id",
                table: "OldschoolRunescapeStats");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeActivities_id",
                table: "OldschoolRunescapeActivities");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OldschoolRunescapeStats",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OldschoolRunescapeActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }
    }
}
