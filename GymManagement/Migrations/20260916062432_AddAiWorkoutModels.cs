using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddAiWorkoutModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GymWorkoutVideos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GymId = table.Column<int>(type: "int", nullable: true),
                    ExerciseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    VideoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GymWorkoutVideos", x => x.Id);
                    table.CheckConstraint("CK_GymWorkoutVideos_ExerciseType", "[ExerciseType] IN ('Squat','PushUp')");
                    table.ForeignKey(
                        name: "FK_GymWorkoutVideos_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkoutSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GymId = table.Column<int>(type: "int", nullable: true),
                    ExerciseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalReps = table.Column<int>(type: "int", nullable: false),
                    ValidReps = table.Column<int>(type: "int", nullable: false),
                    InvalidReps = table.Column<int>(type: "int", nullable: false),
                    AverageFormScore = table.Column<double>(type: "float", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    CaloriesBurned = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkoutSessions", x => x.Id);
                    table.CheckConstraint("CK_WorkoutSessions_ExerciseType", "[ExerciseType] IN ('Squat','PushUp')");
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkoutSessions_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutVideos_ExerciseType_IsDefault",
                table: "GymWorkoutVideos",
                columns: new[] { "ExerciseType", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_GymWorkoutVideos_GymId",
                table: "GymWorkoutVideos",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_GymId_CreatedAt",
                table: "WorkoutSessions",
                columns: new[] { "GymId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkoutSessions_MemberId_CreatedAt",
                table: "WorkoutSessions",
                columns: new[] { "MemberId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GymWorkoutVideos");

            migrationBuilder.DropTable(
                name: "WorkoutSessions");
        }
    }
}
