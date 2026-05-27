using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodea.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class BadgesByDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchdayBadges_UserId_TournamentId_Matchday",
                table: "MatchdayBadges");

            migrationBuilder.DropColumn(
                name: "Matchday",
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MatchdayBadges_UserId_TournamentId_Date",
                table: "MatchdayBadges");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "MatchdayBadges");

            migrationBuilder.AddColumn<int>(
                name: "Matchday",
                table: "MatchdayBadges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MatchdayBadges_UserId_TournamentId_Matchday",
                table: "MatchdayBadges",
                columns: new[] { "UserId", "TournamentId", "Matchday" },
                unique: true);
        }
    }
}
