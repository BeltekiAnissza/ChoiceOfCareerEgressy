using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChoiceOfCareerEgressy.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Settings_EventId",
                table: "TimeSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Settings",
                table: "Settings");

            migrationBuilder.RenameTable(
                name: "Settings",
                newName: "Events");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Events",
                table: "Events",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Events_EventId",
                table: "TimeSlots",
                column: "EventId",
                principalTable: "Events",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeSlots_Events_EventId",
                table: "TimeSlots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Events",
                table: "Events");

            migrationBuilder.RenameTable(
                name: "Events",
                newName: "Settings");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Settings",
                table: "Settings",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeSlots_Settings_EventId",
                table: "TimeSlots",
                column: "EventId",
                principalTable: "Settings",
                principalColumn: "EventId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
