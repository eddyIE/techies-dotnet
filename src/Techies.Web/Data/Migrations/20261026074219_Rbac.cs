using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Techies.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class Rbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "role_id",
                table: "staff",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    module = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_system = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    permission_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                    table.ForeignKey(
                        name: "fk_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permissions",
                columns: new[] { "id", "code", "module", "name" },
                values: new object[,]
                {
                    { 1, "dashboard.view", "Tổng quan", "Xem bảng điều khiển & doanh thu" },
                    { 2, "products.view", "Sản phẩm", "Xem sản phẩm" },
                    { 3, "products.edit", "Sản phẩm", "Thêm / sửa sản phẩm" },
                    { 4, "products.toggle", "Sản phẩm", "Bật / tắt kinh doanh" },
                    { 5, "categories.manage", "Danh mục", "Quản lý danh mục" },
                    { 6, "inventory.view", "Kho", "Xem tồn kho" },
                    { 7, "inventory.import", "Kho", "Nhập hàng" },
                    { 8, "inventory.count", "Kho", "Kiểm kê" },
                    { 9, "orders.view", "Đơn hàng", "Xem đơn hàng" },
                    { 10, "orders.complete", "Đơn hàng", "Hoàn tất đơn hàng" },
                    { 11, "orders.cancel", "Đơn hàng", "Hủy đơn hàng" },
                    { 12, "orders.print", "Đơn hàng", "In phiếu giao hàng" },
                    { 13, "customers.view", "Khách hàng", "Xem khách hàng" },
                    { 14, "customers.lock", "Khách hàng", "Khóa / mở khóa khách hàng" },
                    { 15, "reviews.view", "Đánh giá", "Xem đánh giá" },
                    { 16, "giftcounter.lookup", "Quầy nhận quà", "Tra cứu mã nhận quà" },
                    { 17, "giftcounter.claim", "Quầy nhận quà", "Xác nhận giao quà" },
                    { 18, "coupons.view", "Mã giảm giá", "Xem mã giảm giá" },
                    { 19, "coupons.manage", "Mã giảm giá", "Tạo / sửa / tạm dừng mã" },
                    { 20, "gifts.view", "Quà tặng", "Xem quà tặng" },
                    { 21, "gifts.manage", "Quà tặng", "Thêm / sửa quà tặng" },
                    { 22, "tiers.manage", "Hạng thành viên", "Cấu hình hạng thành viên" }
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "description", "is_system", "name" },
                values: new object[,]
                {
                    { 1, "SUPER_ADMIN", "Toàn quyền, quản lý nhân sự và phân quyền.", true, "Quản trị viên cao cấp" },
                    { 2, "CATALOG_STAFF", "Thêm/sửa sản phẩm, thông số, nhập hàng và kiểm kê.", false, "Nhân viên Kho & Sản phẩm" },
                    { 3, "ORDER_STAFF", "Xử lý đơn hàng, in phiếu giao, tra cứu quà tặng, hỗ trợ khách hàng.", false, "Nhân viên Điều phối & Quầy" }
                });

            migrationBuilder.InsertData(
                table: "role_permissions",
                columns: new[] { "permission_id", "role_id" },
                values: new object[,]
                {
                    { 2, 2 },
                    { 3, 2 },
                    { 4, 2 },
                    { 5, 2 },
                    { 6, 2 },
                    { 7, 2 },
                    { 8, 2 },
                    { 9, 3 },
                    { 10, 3 },
                    { 11, 3 },
                    { 12, 3 },
                    { 13, 3 },
                    { 14, 3 },
                    { 15, 3 },
                    { 16, 3 },
                    { 17, 3 }
                });

            // Existing staff keep their access: the old fixed role becomes the matching seeded role.
            migrationBuilder.Sql("""
                UPDATE staff SET role_id = CASE role
                    WHEN 'SUPER_ADMIN' THEN 1 WHEN 'CATALOG_STAFF' THEN 2 ELSE 3 END;
                """);
            // Roles were inserted with explicit ids; move the identity past them.
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('roles', 'id'), (SELECT max(id) FROM roles));");

            migrationBuilder.DropColumn(
                name: "role",
                table: "staff");

            migrationBuilder.CreateIndex(
                name: "ix_staff_role_id",
                table: "staff",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "ix_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_name",
                table: "roles",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_staff_roles_role_id",
                table: "staff",
                column: "role_id",
                principalTable: "roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_staff_roles_role_id",
                table: "staff");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropIndex(
                name: "ix_staff_role_id",
                table: "staff");

            migrationBuilder.DropColumn(
                name: "role_id",
                table: "staff");

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "staff",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");
        }
    }
}
