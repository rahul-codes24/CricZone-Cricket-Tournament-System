using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KplTournament.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBallByBallFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Innings",
                table: "BallByBalls",
                newName: "IsLegalBall");

            migrationBuilder.RenameColumn(
                name: "ExtraRuns",
                table: "BallByBalls",
                newName: "InningsNumber");

            migrationBuilder.RenameColumn(
                name: "BallResult",
                table: "BallByBalls",
                newName: "BallText");

            migrationBuilder.AddColumn<bool>(
                name: "IsOverCompleted",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Extras",
                table: "BallByBalls",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WicketType",
                table: "BallByBalls",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOverCompleted",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Extras",
                table: "BallByBalls");

            migrationBuilder.DropColumn(
                name: "WicketType",
                table: "BallByBalls");

            migrationBuilder.RenameColumn(
                name: "IsLegalBall",
                table: "BallByBalls",
                newName: "Innings");

            migrationBuilder.RenameColumn(
                name: "InningsNumber",
                table: "BallByBalls",
                newName: "ExtraRuns");

            migrationBuilder.RenameColumn(
                name: "BallText",
                table: "BallByBalls",
                newName: "BallResult");
        }
    }
}
