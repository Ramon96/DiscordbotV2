using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceSnapshotTimestampIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Build CONCURRENTLY so the index build doesn't lock writes on this large, actively
            // written table (the price fetcher inserts every 5 minutes). CONCURRENTLY cannot run
            // inside a transaction, hence suppressTransaction.
            migrationBuilder.Sql(
                "CREATE INDEX CONCURRENTLY IF NOT EXISTS \"IX_OsrsPriceSnapshots_Timestamp\" ON \"OsrsPriceSnapshots\" (\"Timestamp\");",
                suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP INDEX CONCURRENTLY IF EXISTS \"IX_OsrsPriceSnapshots_Timestamp\";",
                suppressTransaction: true);
        }
    }
}
