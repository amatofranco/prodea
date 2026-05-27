using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodea.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTournamentIdFromPrediction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_Tournaments_TournamentId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_TournamentId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_UserId_TournamentId_MatchId",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "TournamentId",
                table: "Predictions");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId_MatchId",
                table: "Predictions",
                columns: new[] { "UserId", "MatchId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Predictions_UserId_MatchId",
                table: "Predictions");

            migrationBuilder.AddColumn<int>(
                name: "TournamentId",
                table: "Predictions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_TournamentId",
                table: "Predictions",
                column: "TournamentId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_UserId_TournamentId_MatchId",
                table: "Predictions",
                columns: new[] { "UserId", "TournamentId", "MatchId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_Tournaments_TournamentId",
                table: "Predictions",
                column: "TournamentId",
                principalTable: "Tournaments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
