using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceNetProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "FaceMatchScore",
                table: "CheckinLogs",
                type: "float",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MemberFaceProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FaceEmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SampleImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    QualityScore = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberFaceProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberFaceProfiles_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberFaceProfiles_MemberId",
                table: "MemberFaceProfiles",
                column: "MemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberFaceProfiles");

            migrationBuilder.DropColumn(
                name: "FaceMatchScore",
                table: "CheckinLogs");
        }
    }
}
