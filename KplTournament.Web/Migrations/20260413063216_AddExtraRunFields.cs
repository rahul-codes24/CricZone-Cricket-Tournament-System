using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KplTournament.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddExtraRunFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentLegByeRuns",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentNoBallRuns",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentWideRuns",
                table: "Matches",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentLegByeRuns",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CurrentNoBallRuns",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "CurrentWideRuns",
                table: "Matches");
        }
    }
}
