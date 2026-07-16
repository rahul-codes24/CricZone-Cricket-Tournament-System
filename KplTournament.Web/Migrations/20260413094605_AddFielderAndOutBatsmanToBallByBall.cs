using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KplTournament.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddFielderAndOutBatsmanToBallByBall : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FielderId",
                table: "BallByBalls",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutBatsmanId",
                table: "BallByBalls",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BallByBalls_FielderId",
                table: "BallByBalls",
                column: "FielderId");

            migrationBuilder.CreateIndex(
                name: "IX_BallByBalls_OutBatsmanId",
                table: "BallByBalls",
                column: "OutBatsmanId");

            migrationBuilder.AddForeignKey(
                name: "FK_BallByBalls_Players_FielderId",
                table: "BallByBalls",
                column: "FielderId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BallByBalls_Players_OutBatsmanId",
                table: "BallByBalls",
                column: "OutBatsmanId",
                principalTable: "Players",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BallByBalls_Players_FielderId",
                table: "BallByBalls");

            migrationBuilder.DropForeignKey(
                name: "FK_BallByBalls_Players_OutBatsmanId",
                table: "BallByBalls");

            migrationBuilder.DropIndex(
                name: "IX_BallByBalls_FielderId",
                table: "BallByBalls");

            migrationBuilder.DropIndex(
                name: "IX_BallByBalls_OutBatsmanId",
                table: "BallByBalls");

            migrationBuilder.DropColumn(
                name: "FielderId",
                table: "BallByBalls");

            migrationBuilder.DropColumn(
                name: "OutBatsmanId",
                table: "BallByBalls");
        }
    }
}
