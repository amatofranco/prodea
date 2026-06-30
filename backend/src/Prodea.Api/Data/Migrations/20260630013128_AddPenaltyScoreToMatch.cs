using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodea.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPenaltyScoreToMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AwayPenaltyScore",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HomePenaltyScore",
                table: "Matches",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AwayPenaltyScore",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "HomePenaltyScore",
                table: "Matches");
        }
    }
}
