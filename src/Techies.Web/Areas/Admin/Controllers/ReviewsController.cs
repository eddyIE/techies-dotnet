using Microsoft.AspNetCore.Mvc;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.Services;
using Techies.Web.ViewModels.Customers;

namespace Techies.Web.Areas.Admin.Controllers;

[RequirePermission(Permissions.ReviewsView)]
public sealed class ReviewsController(ReviewQueries reviews, CurrentStaff staff) : AdminController
{
    public async Task<IActionResult> Index()
    {
        var request = GridRequest.From(Request.Path, Request.QueryString, ReviewQueries.GridSorts, "date", true);
        var (rows, total) = await reviews.GridAsync(request);
        var canSeeCustomers = await staff.HasAsync(Permissions.CustomersView);
        return GridView(new GridModel
        {
            Id = "reviews", Request = request, Total = total, RowsPartial = "_ReviewRows",
            KeywordPlaceholder = "Tìm trong nội dung nhận xét...",
            Columns = [new("Thời gian", "date"), new("Sản phẩm", "product"), new("Khách hàng"), new("Đánh giá", "rating"), new("Nhận xét")],
            Filters =
            [
                new("product", "Sản phẩm", GridFilterType.Select,
                    (await reviews.ReviewedProductsAsync()).Select(p => new GridOption(p.Id.ToString(), p.Name)).ToList()),
                new("rating", "Số sao (1–5)", GridFilterType.NumberRange),
                new("date", "Ngày đánh giá", GridFilterType.DateRange),
            ],
            Rows = rows.Select(r => (r, RowActions(r, canSeeCustomers))).ToList(),
        });
    }

    private List<GridRowAction> RowActions(ReviewRow r, bool canSeeCustomers)
    {
        var actions = new List<GridRowAction>();
        if (canSeeCustomers) actions.Add(new("Xem khách hàng", Url.Action("Details", "Customers", new { id = r.CustomerId })!));
        actions.Add(new("Xem sản phẩm trên cửa hàng", "/san-pham/" + r.ProductSlug, NewTab: true));
        return actions;
    }
}
