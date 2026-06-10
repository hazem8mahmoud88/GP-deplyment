using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureVote.Migrations
{
    /// <inheritdoc />
    public partial class AddGeographicResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GeographicResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ElectionId = table.Column<int>(type: "int", nullable: false),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    GovernorateId = table.Column<int>(type: "int", nullable: false),
                    ConstituencyId = table.Column<int>(type: "int", nullable: true),
                    VoteCount = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeographicResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeographicResults_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeographicResults_Constituencies_ConstituencyId",
                        column: x => x.ConstituencyId,
                        principalTable: "Constituencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_GeographicResults_Elections_ElectionId",
                        column: x => x.ElectionId,
                        principalTable: "Elections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GeographicResults_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeographicResults_CandidateId",
                table: "GeographicResults",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_GeographicResults_ConstituencyId",
                table: "GeographicResults",
                column: "ConstituencyId");

            migrationBuilder.CreateIndex(
                name: "IX_GeographicResults_ElectionId",
                table: "GeographicResults",
                column: "ElectionId");

            migrationBuilder.CreateIndex(
                name: "IX_GeographicResults_GovernorateId",
                table: "GeographicResults",
                column: "GovernorateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeographicResults");
        }
    }
}
