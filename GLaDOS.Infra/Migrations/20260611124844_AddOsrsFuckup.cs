using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOsrsFuckup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OsrsFuckups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FuckupDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsrsFuckups", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OsrsFuckups_id",
                table: "OsrsFuckups",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OsrsFuckups");
        }
    }
}
