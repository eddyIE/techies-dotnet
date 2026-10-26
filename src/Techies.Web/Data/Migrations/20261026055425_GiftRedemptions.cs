using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Techies.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class GiftRedemptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "gift_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    gift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    points_spent = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    redeemed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claimed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    claimed_by = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_redemptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_gift_redemptions_gifts_gift_id",
                        column: x => x.gift_id,
                        principalTable: "gifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gift_redemptions_staff_claimed_by",
                        column: x => x.claimed_by,
                        principalTable: "staff",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_gift_redemptions_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gift_redemptions_claimed_by",
                table: "gift_redemptions",
                column: "claimed_by");

            migrationBuilder.CreateIndex(
                name: "ix_gift_redemptions_code",
                table: "gift_redemptions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gift_redemptions_customer_id",
                table: "gift_redemptions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_gift_redemptions_gift_id",
                table: "gift_redemptions",
                column: "gift_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gift_redemptions");
        }
    }
}
