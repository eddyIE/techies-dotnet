using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Techies.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Identity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "membership_tiers",
                columns: table => new
                {
                    tier = table.Column<short>(type: "smallint", nullable: false),
                    name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    threshold_points = table.Column<long>(type: "bigint", nullable: false),
                    discount_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_membership_tiers", x => x.tier);
                });

            migrationBuilder.CreateTable(
                name: "staff",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(72)", maxLength: 72, nullable: false),
                    full_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "addresses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    phone = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    line1 = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ward = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    district = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    province = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_addresses", x => x.id);
                    table.ForeignKey(
                        name: "fk_addresses_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_loyalty",
                columns: table => new
                {
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lifetime_points = table.Column<long>(type: "bigint", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    tier = table.Column<short>(type: "smallint", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_loyalty", x => x.customer_id);
                    table.ForeignKey(
                        name: "fk_customer_loyalty_membership_tiers_tier",
                        column: x => x.tier,
                        principalTable: "membership_tiers",
                        principalColumn: "tier",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_loyalty_users_customer_id",
                        column: x => x.customer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_addresses_user_id",
                table: "addresses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_loyalty_tier",
                table: "customer_loyalty",
                column: "tier");

            migrationBuilder.CreateIndex(
                name: "ix_staff_email",
                table: "staff",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "addresses");

            migrationBuilder.DropTable(
                name: "customer_loyalty");

            migrationBuilder.DropTable(
                name: "staff");

            migrationBuilder.DropTable(
                name: "membership_tiers");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
