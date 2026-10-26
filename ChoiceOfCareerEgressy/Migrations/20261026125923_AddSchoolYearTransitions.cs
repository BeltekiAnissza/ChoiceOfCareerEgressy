using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoiceOfCareerEgressy.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolYearTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SchoolYearTransitions",
                columns: table => new
                {
                    SchoolYearTransitionId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CalendarYear = table.Column<int>(type: "INTEGER", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExecutedByUserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolYearTransitions", x => x.SchoolYearTransitionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolYearTransitions_CalendarYear",
                table: "SchoolYearTransitions",
                column: "CalendarYear",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolYearTransitions");
        }
    }
}
