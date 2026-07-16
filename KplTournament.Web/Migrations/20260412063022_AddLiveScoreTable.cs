using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KplTournament.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveScoreTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LiveScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    Wickets = table.Column<int>(type: "INTEGER", nullable: false),
                    Overs = table.Column<int>(type: "INTEGER", nullable: false),
                    Balls = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentInnings = table.Column<int>(type: "INTEGER", nullable: false),
                    StrikerName = table.Column<string>(type: "TEXT", nullable: false),
                    NonStrikerName = table.Column<string>(type: "TEXT", nullable: false),
                    BowlerName = table.Column<string>(type: "TEXT", nullable: false),
                    StrikerRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    StrikerBalls = table.Column<int>(type: "INTEGER", nullable: false),
                    NonStrikerRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    NonStrikerBalls = table.Column<int>(type: "INTEGER", nullable: false),
                    BowlerRunsGiven = table.Column<int>(type: "INTEGER", nullable: false),
                    BowlerWickets = table.Column<int>(type: "INTEGER", nullable: false),
                    BowlerBalls = table.Column<int>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveScores", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveScores");
        }
    }
}
