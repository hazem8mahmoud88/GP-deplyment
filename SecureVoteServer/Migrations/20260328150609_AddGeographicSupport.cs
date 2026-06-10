using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVote.Migrations
{
    /// <inheritdoc />
    public partial class AddGeographicSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConstituencyId",
                table: "Candidates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_ConstituencyId",
                table: "Candidates",
                column: "ConstituencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Constituencies_ConstituencyId",
                table: "Candidates",
                column: "ConstituencyId",
                principalTable: "Constituencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Constituencies_ConstituencyId",
                table: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_ConstituencyId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "ConstituencyId",
                table: "Candidates");
        }
    }
}
