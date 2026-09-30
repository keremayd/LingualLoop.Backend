using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingualLoop.Api.Migrations;

public partial class AddLeaguePromotionAcknowledgement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE user_league_progress
                ADD COLUMN IF NOT EXISTS announced_league_rank integer;

            UPDATE user_league_progress
            SET announced_league_rank = CASE
                WHEN points >= 120 THEN 11
                WHEN points >= 90 THEN 10
                WHEN points >= 80 THEN 9
                WHEN points >= 70 THEN 8
                WHEN points >= 60 THEN 7
                WHEN points >= 50 THEN 6
                WHEN points >= 40 THEN 5
                WHEN points >= 30 THEN 4
                WHEN points >= 20 THEN 3
                WHEN points >= 10 THEN 2
                ELSE 1
            END
            WHERE announced_league_rank IS NULL;

            ALTER TABLE user_league_progress
                ALTER COLUMN announced_league_rank SET DEFAULT 1,
                ALTER COLUMN announced_league_rank SET NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE user_league_progress
                DROP COLUMN IF EXISTS announced_league_rank;
            """);
    }
}
