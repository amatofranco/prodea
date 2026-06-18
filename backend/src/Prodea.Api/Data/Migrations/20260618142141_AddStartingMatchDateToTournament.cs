using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prodea.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStartingMatchDateToTournament : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usamos IF NOT EXISTS porque algunas de estas columnas ya fueron aplicadas
            // a mano vía ALTER TABLE en el startup de la app (ver Program.cs) antes de
            // que existiera una migración formal para ellas.
            migrationBuilder.Sql(@"ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""IsPwa"" boolean NOT NULL DEFAULT FALSE;");

            migrationBuilder.Sql(@"ALTER TABLE ""Tournaments"" ADD COLUMN IF NOT EXISTS ""StartingMatchDate"" timestamp with time zone NOT NULL DEFAULT '0001-01-01T00:00:00';");

            migrationBuilder.Sql(@"UPDATE ""Tournaments"" SET ""StartingMatchDate"" = ""CreatedAt"";");

            migrationBuilder.Sql(@"ALTER TABLE ""Matches"" ADD COLUMN IF NOT EXISTS ""FinishedAt"" timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""Matches"" ADD COLUMN IF NOT EXISTS ""GoalsJson"" text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Matches"" ADD COLUMN IF NOT EXISTS ""LastUpdatedAt"" timestamp with time zone;");

            migrationBuilder.Sql(@"ALTER TABLE ""Matches"" ADD COLUMN IF NOT EXISTS ""LivePhase"" text;");

            migrationBuilder.Sql(@"ALTER TABLE ""Matches"" ADD COLUMN IF NOT EXISTS ""StartedAt"" timestamp with time zone;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPwa",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StartingMatchDate",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "GoalsJson",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "LivePhase",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Matches");
        }
    }
}
