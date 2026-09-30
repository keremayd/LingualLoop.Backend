using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Postgres;

#nullable disable

namespace LingualLoop.Api.Migrations;

[DbContext(typeof(LingualLoopContext))]
[Migration("20260622160000_AddArticlePractice")]
public partial class AddArticlePractice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "article",
            table: "karty",
            type: "text",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "noun_text",
            table: "karty",
            type: "text",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE karty
            SET article = lower(split_part(trim(CASE WHEN nullif(trim(correct_text), '') IS NOT NULL THEN correct_text ELSE question_text END), ' ', 1)),
                noun_text = regexp_replace(trim(CASE WHEN nullif(trim(correct_text), '') IS NOT NULL THEN correct_text ELSE question_text END), '^(der|die|das)\s+', '', 'i');
            """);

        migrationBuilder.CreateTable(
            name: "user_karty_learning",
            columns: table => new
            {
                user_karty_learning_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                user_id = table.Column<string>(type: "text", nullable: false),
                karty_id = table.Column<int>(type: "integer", nullable: false),
                correct_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                article_attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                article_correct_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                consecutive_serve_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                first_learned_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                last_correct_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                last_article_served_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                last_article_answered_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_karty_learning", x => x.user_karty_learning_id);
                table.ForeignKey("FK_user_karty_learning_user", x => x.user_id, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_user_karty_learning_karty", x => x.karty_id, "karty", "karty_id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_user_karty_learning_user_id_karty_id",
            table: "user_karty_learning",
            columns: new[] { "user_id", "karty_id" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_karty_learning");
        migrationBuilder.DropColumn(name: "article", table: "karty");
        migrationBuilder.DropColumn(name: "noun_text", table: "karty");
    }
}
