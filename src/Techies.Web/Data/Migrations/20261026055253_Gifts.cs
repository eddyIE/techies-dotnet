using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Techies.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Gifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gifts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    points_cost = table.Column<long>(type: "bigint", nullable: false),
                    min_tier = table.Column<short>(type: "smallint", nullable: false),
                    stock_available = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gifts", x => x.id);
                    table.CheckConstraint("ck_gifts_points_cost", "points_cost > 0");
                    table.CheckConstraint("ck_gifts_stock_available", "stock_available >= 0");
                    table.ForeignKey(
                        name: "fk_gifts_membership_tiers_min_tier",
                        column: x => x.min_tier,
                        principalTable: "membership_tiers",
                        principalColumn: "tier",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gifts_min_tier",
                table: "gifts",
                column: "min_tier");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gifts");
        }
    }
}
