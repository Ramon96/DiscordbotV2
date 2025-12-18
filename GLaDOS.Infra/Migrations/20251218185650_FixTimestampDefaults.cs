using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GLaDOS.Infra.Migrations
{
    /// <inheritdoc />
    public partial class FixTimestampDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""DiscordUsers"" 
                ALTER COLUMN ""created"" SET DEFAULT CURRENT_TIMESTAMP;
                
                ALTER TABLE ""DiscordUsers"" 
                ALTER COLUMN ""modified"" SET DEFAULT CURRENT_TIMESTAMP;
                
                ALTER TABLE ""OldschoolRunescapeUsers"" 
                ALTER COLUMN ""created"" SET DEFAULT CURRENT_TIMESTAMP;
                
                ALTER TABLE ""OldschoolRunescapeUsers"" 
                ALTER COLUMN ""modified"" SET DEFAULT CURRENT_TIMESTAMP;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""DiscordUsers"" 
                ALTER COLUMN ""created"" DROP DEFAULT;
                
                ALTER TABLE ""DiscordUsers"" 
                ALTER COLUMN ""modified"" DROP DEFAULT;
                
                ALTER TABLE ""OldschoolRunescapeUsers"" 
                ALTER COLUMN ""created"" DROP DEFAULT;
                
                ALTER TABLE ""OldschoolRunescapeUsers"" 
                ALTER COLUMN ""modified"" DROP DEFAULT;
            ");
        }
    }
}
