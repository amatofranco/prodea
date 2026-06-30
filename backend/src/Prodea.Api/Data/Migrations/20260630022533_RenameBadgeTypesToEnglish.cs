using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodea.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameBadgeTypesToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MatchdayBadges — rename BadgeType values from Spanish to English
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'MVP' WHERE \"BadgeType\" = 'Crack'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Jinx' WHERE \"BadgeType\" = 'Mufa'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Oracle' WHERE \"BadgeType\" = 'Adivino'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Sniper' WHERE \"BadgeType\" = 'Francotirador'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Choker' WHERE \"BadgeType\" = 'PechoFrio'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Goalscorer' WHERE \"BadgeType\" = 'Goleador'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Miser' WHERE \"BadgeType\" = 'Rustico'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Wobbler' WHERE \"BadgeType\" = 'Tambaleante'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Clown' WHERE \"BadgeType\" = 'Payaso'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Sleeper' WHERE \"BadgeType\" = 'Dormido'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Lukewarm' WHERE \"BadgeType\" = 'Tibio'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Champion' WHERE \"BadgeType\" = 'Campeon'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'RunnerUp' WHERE \"BadgeType\" = 'Subcampeon'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'ThirdPlace' WHERE \"BadgeType\" = 'TercerPuesto'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'LastPlace' WHERE \"BadgeType\" = 'Ultimo'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'SecondToLast' WHERE \"BadgeType\" = 'Penultimo'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'TournamentGoalscorer' WHERE \"BadgeType\" = 'GoleadorTorneo'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'TournamentMiser' WHERE \"BadgeType\" = 'RusticoTorneo'");

            // AccumulativeBadges — rename BadgeType values from Spanish to English
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TotalChoke' WHERE \"BadgeType\" = 'PecheadaTotal'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'HotStreak' WHERE \"BadgeType\" = 'RachaInfernal'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TheWall' WHERE \"BadgeType\" = 'ElMuro'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TheGhost' WHERE \"BadgeType\" = 'ElFantasma'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TripleJinx' WHERE \"BadgeType\" = 'TripleMufa'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TotalLukewarm' WHERE \"BadgeType\" = 'TibiezaTotal'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'SerialGoalscorer' WHERE \"BadgeType\" = 'GoleadorSerial'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TotalMiser' WHERE \"BadgeType\" = 'RusticoTotal'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // MatchdayBadges — revert BadgeType values from English back to Spanish
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Crack' WHERE \"BadgeType\" = 'MVP'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Mufa' WHERE \"BadgeType\" = 'Jinx'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Adivino' WHERE \"BadgeType\" = 'Oracle'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Francotirador' WHERE \"BadgeType\" = 'Sniper'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'PechoFrio' WHERE \"BadgeType\" = 'Choker'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Goleador' WHERE \"BadgeType\" = 'Goalscorer'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Rustico' WHERE \"BadgeType\" = 'Miser'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Tambaleante' WHERE \"BadgeType\" = 'Wobbler'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Payaso' WHERE \"BadgeType\" = 'Clown'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Dormido' WHERE \"BadgeType\" = 'Sleeper'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Tibio' WHERE \"BadgeType\" = 'Lukewarm'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Campeon' WHERE \"BadgeType\" = 'Champion'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Subcampeon' WHERE \"BadgeType\" = 'RunnerUp'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'TercerPuesto' WHERE \"BadgeType\" = 'ThirdPlace'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Ultimo' WHERE \"BadgeType\" = 'LastPlace'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'Penultimo' WHERE \"BadgeType\" = 'SecondToLast'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'GoleadorTorneo' WHERE \"BadgeType\" = 'TournamentGoalscorer'");
            migrationBuilder.Sql("UPDATE \"MatchdayBadges\" SET \"BadgeType\" = 'RusticoTorneo' WHERE \"BadgeType\" = 'TournamentMiser'");

            // AccumulativeBadges — revert BadgeType values from English back to Spanish
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'PecheadaTotal' WHERE \"BadgeType\" = 'TotalChoke'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'RachaInfernal' WHERE \"BadgeType\" = 'HotStreak'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'ElMuro' WHERE \"BadgeType\" = 'TheWall'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'ElFantasma' WHERE \"BadgeType\" = 'TheGhost'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TripleMufa' WHERE \"BadgeType\" = 'TripleJinx'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'TibiezaTotal' WHERE \"BadgeType\" = 'TotalLukewarm'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'GoleadorSerial' WHERE \"BadgeType\" = 'SerialGoalscorer'");
            migrationBuilder.Sql("UPDATE \"AccumulativeBadges\" SET \"BadgeType\" = 'RusticoTotal' WHERE \"BadgeType\" = 'TotalMiser'");
        }
    }
}
