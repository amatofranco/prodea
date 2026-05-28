using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Prodea.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BadgesByMatchday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchdayBadges_UserId_TournamentId_Date",
                table: "MatchdayBadges");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "MatchdayBadges");

            migrationBuilder.AddColumn<string>(
                name: "AwayTeamLabel",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HomeTeamLabel",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Minute",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Matchday",
                table: "MatchdayBadges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Phase",
                table: "MatchdayBadges",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PredictionBackups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    JsonData = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionBackups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchdayBadges_UserId_TournamentId_Phase_Matchday",
                table: "MatchdayBadges",
                columns: new[] { "UserId", "TournamentId", "Phase", "Matchday" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionBackups");

            migrationBuilder.DropIndex(
                name: "IX_MatchdayBadges_UserId_TournamentId_Phase_Matchday",
                table: "MatchdayBadges");

            migrationBuilder.DropColumn(
                name: "AwayTeamLabel",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomeTeamLabel",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Minute",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Matchday",
                table: "MatchdayBadges");

            migrationBuilder.DropColumn(
                name: "Phase",
                table: "MatchdayBadges");

            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "MatchdayBadges",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.CreateIndex(
                name: "IX_MatchdayBadges_UserId_TournamentId_Date",
                table: "MatchdayBadges",
                columns: new[] { "UserId", "TournamentId", "Date" },
                unique: true);
        }
    }
}
