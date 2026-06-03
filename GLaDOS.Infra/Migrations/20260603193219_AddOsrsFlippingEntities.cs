using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddOsrsFlippingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OsrsItemMappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OsrsItemId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GeLimit = table.Column<int>(type: "integer", nullable: true),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsrsItemMappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "OsrsPriceSnapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OsrsItemId = table.Column<int>(type: "integer", nullable: false),
                    AvgBuyPrice = table.Column<long>(type: "bigint", nullable: false),
                    AvgSellPrice = table.Column<long>(type: "bigint", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OsrsPriceSnapshots", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OsrsItemMappings_id",
                table: "OsrsItemMappings",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsItemMappings_OsrsItemId",
                table: "OsrsItemMappings",
                column: "OsrsItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsPriceSnapshots_id",
                table: "OsrsPriceSnapshots",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OsrsPriceSnapshots_OsrsItemId_Timestamp",
                table: "OsrsPriceSnapshots",
                columns: new[] { "OsrsItemId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OsrsItemMappings");

            migrationBuilder.DropTable(
                name: "OsrsPriceSnapshots");
        }
    }
}
