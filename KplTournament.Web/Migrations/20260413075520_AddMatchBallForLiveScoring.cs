using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KplTournament.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchBallForLiveScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MatchBalls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    InningsNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    OverNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    BallNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    BowlerName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BatsmanName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Runs = table.Column<int>(type: "INTEGER", nullable: false),
                    Extras = table.Column<int>(type: "INTEGER", nullable: false),
                    IsWide = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsNoBall = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsWicket = table.Column<bool>(type: "INTEGER", nullable: false),
                    WicketType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    BallResult = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchBalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchBalls_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchBalls_MatchId",
                table: "MatchBalls",
                column: "MatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchBalls");
        }
    }
}
