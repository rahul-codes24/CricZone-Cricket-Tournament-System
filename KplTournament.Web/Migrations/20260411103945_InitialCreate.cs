using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KplTournament.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerName = table.Column<string>(type: "TEXT", nullable: false),
                    PrimaryColor = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    IsCaptain = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsViceCaptain = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsInjured = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Venue = table.Column<string>(type: "TEXT", nullable: false),
                    OversPerInnings = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamAId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamBId = table.Column<int>(type: "INTEGER", nullable: false),
                    TossWinner = table.Column<string>(type: "TEXT", nullable: false),
                    TossDecision = table.Column<string>(type: "TEXT", nullable: false),
                    TeamAScore = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamAWickets = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamAOvers = table.Column<decimal>(type: "TEXT", nullable: false),
                    TeamBScore = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamBWickets = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamBOvers = table.Column<decimal>(type: "TEXT", nullable: false),
                    Result = table.Column<string>(type: "TEXT", nullable: false),
                    ResultText = table.Column<string>(type: "TEXT", nullable: true),
                    WinnerTeamId = table.Column<int>(type: "INTEGER", nullable: true),
                    ManOfTheMatchPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentInnings = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentWickets = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentOver = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentBall = table.Column<int>(type: "INTEGER", nullable: false),
                    StrikerId = table.Column<int>(type: "INTEGER", nullable: true),
                    NonStrikerId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentBowlerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Target = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Players_CurrentBowlerId",
                        column: x => x.CurrentBowlerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Players_ManOfTheMatchPlayerId",
                        column: x => x.ManOfTheMatchPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Players_NonStrikerId",
                        column: x => x.NonStrikerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Players_StrikerId",
                        column: x => x.StrikerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_TeamAId",
                        column: x => x.TeamAId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_TeamBId",
                        column: x => x.TeamBId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_WinnerTeamId",
                        column: x => x.WinnerTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BallByBalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    Innings = table.Column<int>(type: "INTEGER", nullable: false),
                    OverNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    BallNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Runs = table.Column<int>(type: "INTEGER", nullable: false),
                    IsWicket = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsWide = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNoBall = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsBye = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLegBye = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExtraRuns = table.Column<int>(type: "INTEGER", nullable: false),
                    BatsmanId = table.Column<int>(type: "INTEGER", nullable: true),
                    BowlerId = table.Column<int>(type: "INTEGER", nullable: true),
                    BallResult = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BallByBalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BallByBalls_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BallByBalls_Players_BatsmanId",
                        column: x => x.BatsmanId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BallByBalls_Players_BowlerId",
                        column: x => x.BowlerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BattingScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Runs = table.Column<int>(type: "INTEGER", nullable: false),
                    Balls = table.Column<int>(type: "INTEGER", nullable: false),
                    Fours = table.Column<int>(type: "INTEGER", nullable: false),
                    Sixes = table.Column<int>(type: "INTEGER", nullable: false),
                    IsOut = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattingScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattingScores_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BattingScores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BattingScores_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BowlingScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    Overs = table.Column<decimal>(type: "TEXT", nullable: false),
                    RunsGiven = table.Column<int>(type: "INTEGER", nullable: false),
                    Wickets = table.Column<int>(type: "INTEGER", nullable: false),
                    Maidens = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BowlingScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BowlingScores_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BowlingScores_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BowlingScores_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BallByBalls_BatsmanId",
                table: "BallByBalls",
                column: "BatsmanId");

            migrationBuilder.CreateIndex(
                name: "IX_BallByBalls_BowlerId",
                table: "BallByBalls",
                column: "BowlerId");

            migrationBuilder.CreateIndex(
                name: "IX_BallByBalls_MatchId",
                table: "BallByBalls",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BattingScores_MatchId",
                table: "BattingScores",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BattingScores_PlayerId",
                table: "BattingScores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_BattingScores_TeamId",
                table: "BattingScores",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_BowlingScores_MatchId",
                table: "BowlingScores",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BowlingScores_PlayerId",
                table: "BowlingScores",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_BowlingScores_TeamId",
                table: "BowlingScores",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_CurrentBowlerId",
                table: "Matches",
                column: "CurrentBowlerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_ManOfTheMatchPlayerId",
                table: "Matches",
                column: "ManOfTheMatchPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_NonStrikerId",
                table: "Matches",
                column: "NonStrikerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_StrikerId",
                table: "Matches",
                column: "StrikerId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamAId",
                table: "Matches",
                column: "TeamAId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamBId",
                table: "Matches",
                column: "TeamBId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_WinnerTeamId",
                table: "Matches",
                column: "WinnerTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "BallByBalls");

            migrationBuilder.DropTable(
                name: "BattingScores");

            migrationBuilder.DropTable(
                name: "BowlingScores");

            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Teams");
        }
    }
}
