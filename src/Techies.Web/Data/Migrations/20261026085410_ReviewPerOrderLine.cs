using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Techies.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReviewPerOrderLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reviews_order_id",
                table: "reviews");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_order_id_product_id",
                table: "reviews",
                columns: new[] { "order_id", "product_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_reviews_order_id_product_id",
                table: "reviews");

            migrationBuilder.CreateIndex(
                name: "ix_reviews_order_id",
                table: "reviews",
                column: "order_id");
        }
    }
}
