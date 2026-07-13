using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddShirtlessOldManPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShirtlessOldManPosts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    TaggedDiscordUserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TaggedUsername = table.Column<string>(type: "text", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShirtlessOldManPosts", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShirtlessOldManPosts_id",
                table: "ShirtlessOldManPosts",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShirtlessOldManPosts_MessageId",
                table: "ShirtlessOldManPosts",
                column: "MessageId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShirtlessOldManPosts");
        }
    }
}
