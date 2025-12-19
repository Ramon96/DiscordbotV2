using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class Patch_001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeActivities_OldschoolRunescapeUsers_id",
                table: "OldschoolRunescapeActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_id",
                table: "OldschoolRunescapeStats");

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeStats_OldschoolRunescapeUserId",
                table: "OldschoolRunescapeStats",
                column: "OldschoolRunescapeUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OldschoolRunescapeActivities_OldschoolRunescapeUserId",
                table: "OldschoolRunescapeActivities",
                column: "OldschoolRunescapeUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeActivities_OldschoolRunescapeUsers_Oldsch~",
                table: "OldschoolRunescapeActivities",
                column: "OldschoolRunescapeUserId",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_OldschoolRu~",
                table: "OldschoolRunescapeStats",
                column: "OldschoolRunescapeUserId",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeActivities_OldschoolRunescapeUsers_Oldsch~",
                table: "OldschoolRunescapeActivities");

            migrationBuilder.DropForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_OldschoolRu~",
                table: "OldschoolRunescapeStats");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeStats_OldschoolRunescapeUserId",
                table: "OldschoolRunescapeStats");

            migrationBuilder.DropIndex(
                name: "IX_OldschoolRunescapeActivities_OldschoolRunescapeUserId",
                table: "OldschoolRunescapeActivities");

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeActivities_OldschoolRunescapeUsers_id",
                table: "OldschoolRunescapeActivities",
                column: "id",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OldschoolRunescapeStats_OldschoolRunescapeUsers_id",
                table: "OldschoolRunescapeStats",
                column: "id",
                principalTable: "OldschoolRunescapeUsers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
