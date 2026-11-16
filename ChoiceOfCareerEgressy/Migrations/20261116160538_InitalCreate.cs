using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ChoiceOfCareerEgressy.Migrations
{
    /// <inheritdoc />
    public partial class InitalCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    AuditLogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.AuditLogId);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.BranchId);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    EventDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRegistrationEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    LocationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationName = table.Column<string>(type: "text", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.LocationId);
                });

            migrationBuilder.CreateTable(
                name: "SchoolYearTransitions",
                columns: table => new
                {
                    SchoolYearTransitionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CalendarYear = table.Column<int>(type: "integer", nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolYearTransitions", x => x.SchoolYearTransitionId);
                });

            migrationBuilder.CreateTable(
                name: "Sexes",
                columns: table => new
                {
                    SexId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SexName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sexes", x => x.SexId);
                });

            migrationBuilder.CreateTable(
                name: "TimeSlots",
                columns: table => new
                {
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeSlotName = table.Column<string>(type: "text", nullable: false),
                    BeginDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSlots", x => x.TimeSlotId);
                    table.ForeignKey(
                        name: "FK_TimeSlots_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    PlanId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanName = table.Column<string>(type: "text", nullable: false),
                    PlanDescription = table.Column<string>(type: "text", nullable: false),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    SexId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.PlanId);
                    table.ForeignKey(
                        name: "FK_Plans_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plans_Sexes_SexId",
                        column: x => x.SexId,
                        principalTable: "Sexes",
                        principalColumn: "SexId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupervisionPlaces",
                columns: table => new
                {
                    SupervisionPlaceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false),
                    RequiredSupervisors = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupervisionPlaces", x => x.SupervisionPlaceId);
                    table.ForeignKey(
                        name: "FK_SupervisionPlaces_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "TimeSlotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanSessions",
                columns: table => new
                {
                    PlanSessionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlanId = table.Column<int>(type: "integer", nullable: false),
                    TimeSlotId = table.Column<int>(type: "integer", nullable: false),
                    LocationId = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    RequiredSupervisors = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanSessions", x => x.PlanSessionId);
                    table.ForeignKey(
                        name: "FK_PlanSessions_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "LocationId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanSessions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "PlanId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanSessions_TimeSlots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalTable: "TimeSlots",
                        principalColumn: "TimeSlotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    ApplicationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PlanSessionId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.ApplicationId);
                    table.ForeignKey(
                        name: "FK_Applications_PlanSessions_PlanSessionId",
                        column: x => x.PlanSessionId,
                        principalTable: "PlanSessions",
                        principalColumn: "PlanSessionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    GradeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GradeName = table.Column<string>(type: "text", nullable: false),
                    HomeroomTeacherId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.GradeId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    EmailAddress = table.Column<string>(type: "text", nullable: false),
                    EmailSearchHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OmIdentifierHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    GradeId = table.Column<int>(type: "integer", nullable: true),
                    BranchId = table.Column<int>(type: "integer", nullable: true),
                    SexId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Grades_GradeId",
                        column: x => x.GradeId,
                        principalTable: "Grades",
                        principalColumn: "GradeId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Sexes_SexId",
                        column: x => x.SexId,
                        principalTable: "Sexes",
                        principalColumn: "SexId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Supervisions",
                columns: table => new
                {
                    SupervisionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PlanSessionId = table.Column<int>(type: "integer", nullable: true),
                    SupervisionPlaceId = table.Column<int>(type: "integer", nullable: true),
                    ApplicationDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdateDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Supervisions", x => x.SupervisionId);
                    table.CheckConstraint("CK_Supervision_ExactlyOneTarget", "(\"PlanSessionId\" IS NOT NULL AND \"SupervisionPlaceId\" IS NULL) OR (\"PlanSessionId\" IS NULL AND \"SupervisionPlaceId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Supervisions_PlanSessions_PlanSessionId",
                        column: x => x.PlanSessionId,
                        principalTable: "PlanSessions",
                        principalColumn: "PlanSessionId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Supervisions_SupervisionPlaces_SupervisionPlaceId",
                        column: x => x.SupervisionPlaceId,
                        principalTable: "SupervisionPlaces",
                        principalColumn: "SupervisionPlaceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Supervisions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_PlanSessionId",
                table: "Applications",
                column: "PlanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_UserId_PlanSessionId",
                table: "Applications",
                columns: new[] { "UserId", "PlanSessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Branches_BranchName",
                table: "Branches",
                column: "BranchName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_GradeName",
                table: "Grades",
                column: "GradeName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Grades_HomeroomTeacherId",
                table: "Grades",
                column: "HomeroomTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationName",
                table: "Locations",
                column: "LocationName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plans_BranchId",
                table: "Plans",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_SexId",
                table: "Plans",
                column: "SexId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanSessions_LocationId_TimeSlotId",
                table: "PlanSessions",
                columns: new[] { "LocationId", "TimeSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanSessions_PlanId_TimeSlotId",
                table: "PlanSessions",
                columns: new[] { "PlanId", "TimeSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlanSessions_TimeSlotId",
                table: "PlanSessions",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolYearTransitions_CalendarYear",
                table: "SchoolYearTransitions",
                column: "CalendarYear",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sexes_SexName",
                table: "Sexes",
                column: "SexName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisionPlaces_Name_TimeSlotId",
                table: "SupervisionPlaces",
                columns: new[] { "Name", "TimeSlotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupervisionPlaces_TimeSlotId",
                table: "SupervisionPlaces",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Supervisions_PlanSessionId",
                table: "Supervisions",
                column: "PlanSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Supervisions_SupervisionPlaceId",
                table: "Supervisions",
                column: "SupervisionPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Supervisions_UserId",
                table: "Supervisions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSlots_EventId",
                table: "TimeSlots",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_BranchId",
                table: "Users",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailSearchHash",
                table: "Users",
                column: "EmailSearchHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_GradeId",
                table: "Users",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OmIdentifierHash",
                table: "Users",
                column: "OmIdentifierHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_SexId",
                table: "Users",
                column: "SexId");

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Users_UserId",
                table: "Applications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Users_HomeroomTeacherId",
                table: "Grades",
                column: "HomeroomTeacherId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Users_HomeroomTeacherId",
                table: "Grades");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "SchoolYearTransitions");

            migrationBuilder.DropTable(
                name: "Supervisions");

            migrationBuilder.DropTable(
                name: "PlanSessions");

            migrationBuilder.DropTable(
                name: "SupervisionPlaces");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "TimeSlots");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Sexes");
        }
    }
}
