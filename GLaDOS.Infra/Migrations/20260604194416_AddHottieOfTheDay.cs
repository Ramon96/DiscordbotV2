using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddHottieOfTheDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HottieOfTheDays",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DateAwarded = table.Column<DateOnly>(type: "date", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HottieOfTheDays", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HottieOfTheDays_DiscordUserId_DateAwarded",
                table: "HottieOfTheDays",
                columns: new[] { "DiscordUserId", "DateAwarded" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HottieOfTheDays_id",
                table: "HottieOfTheDays",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HottieOfTheDays");
        }
    }
}
