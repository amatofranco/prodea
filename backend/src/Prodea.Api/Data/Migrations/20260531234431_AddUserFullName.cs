using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodea.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFullName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChampionPicks_Tournaments_TournamentId",
                table: "ChampionPicks");

            migrationBuilder.DropIndex(
                name: "IX_ChampionPicks_TournamentId_UserId",
                table: "ChampionPicks");

            migrationBuilder.DropIndex(
                name: "IX_ChampionPicks_UserId",
                table: "ChampionPicks");

            migrationBuilder.DropColumn(
                name: "TournamentId",
                table: "ChampionPicks");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredictedPenaltyWinner",
                table: "Predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Winner",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChampionPicks_UserId",
                table: "ChampionPicks",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChampionPicks_UserId",
                table: "ChampionPicks");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PredictedPenaltyWinner",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "Winner",
                table: "Matches");

            migrationBuilder.AddColumn<int>(
                name: "TournamentId",
                table: "ChampionPicks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ChampionPicks_TournamentId_UserId",
                table: "ChampionPicks",
                columns: new[] { "TournamentId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChampionPicks_UserId",
                table: "ChampionPicks",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChampionPicks_Tournaments_TournamentId",
                table: "ChampionPicks",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
