using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LingualLoop.Api.Migrations
{
    public partial class AddUserKartyWrongHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_karty_history",
                columns: table => new
                {
                    user_karty_history_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    karty_id = table.Column<int>(type: "integer", nullable: false),
                    wrong_count = table.Column<int>(type: "integer", nullable: false),
                    last_wrong_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reviewed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_karty_history", x => x.user_karty_history_id);
                    table.ForeignKey(
                        name: "FK_user_karty_history_AspNetUsers_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_karty_history_karty_karty_id",
                        column: x => x.karty_id,
                        principalTable: "karty",
                        principalColumn: "karty_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_karty_history_karty_id",
                table: "user_karty_history",
                column: "karty_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_karty_history_user_id_karty_id",
                table: "user_karty_history",
                columns: new[] { "user_id", "karty_id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_karty_history");
        }
    }
}
