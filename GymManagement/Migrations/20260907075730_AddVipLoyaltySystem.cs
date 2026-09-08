using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Migrations
{
    /// <inheritdoc />
    public partial class AddVipLoyaltySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VipTierSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GymId = table.Column<int>(type: "int", nullable: false),
                    TierName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinPurchaseCount = table.Column<int>(type: "int", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    BenefitDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BadgeColor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VipTierSettings", x => x.Id);
                    table.CheckConstraint("CK_VipTierSettings_DiscountPercent", "[DiscountPercent] IS NULL OR ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100)");
                    table.CheckConstraint("CK_VipTierSettings_MinPurchaseCount", "[MinPurchaseCount] > 0");
                    table.ForeignKey(
                        name: "FK_VipTierSettings_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MemberVipStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MemberId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GymId = table.Column<int>(type: "int", nullable: false),
                    CurrentTierId = table.Column<int>(type: "int", nullable: true),
                    TotalPurchaseCount = table.Column<int>(type: "int", nullable: false),
                    AchievedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPurchaseAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberVipStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberVipStatuses_AspNetUsers_MemberId",
                        column: x => x.MemberId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberVipStatuses_Gyms_GymId",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MemberVipStatuses_VipTierSettings_CurrentTierId",
                        column: x => x.CurrentTierId,
                        principalTable: "VipTierSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberVipStatuses_CurrentTierId",
                table: "MemberVipStatuses",
                column: "CurrentTierId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberVipStatuses_GymId",
                table: "MemberVipStatuses",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberVipStatuses_MemberId_GymId",
                table: "MemberVipStatuses",
                columns: new[] { "MemberId", "GymId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VipTierSettings_GymId",
                table: "VipTierSettings",
                column: "GymId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberVipStatuses");

            migrationBuilder.DropTable(
                name: "VipTierSettings");
        }
    }
}
