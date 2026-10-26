using Microsoft.AspNetCore.Http;
using Techies.Web.Infrastructure.Grid;

namespace Techies.Tests.Unit;

public class GridRequestTests
{
    private static GridRequest Parse(string query) =>
        GridRequest.From(new PathString("/admin/products"), QueryString.Create(Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(query)
            .SelectMany(kv => kv.Value.Select(v => new KeyValuePair<string, string?>(kv.Key, v)))),
            sortable: ["name", "price"], defaultSort: "name", defaultDesc: false);

    [Fact]
    public void Unknown_sort_falls_back_to_default()
    {
        var r = Parse("?sort=password&dir=desc");

        Assert.Equal(("name", false), (r.Sort, r.Desc));
    }

    [Fact]
    public void Known_sort_and_direction_are_kept()
    {
        var r = Parse("?sort=price&dir=desc");

        Assert.Equal(("price", true), (r.Sort, r.Desc));
    }

    [Theory]
    [InlineData("", 20)]
    [InlineData("?size=50", 50)]
    [InlineData("?size=100", 100)]
    [InlineData("?size=5000", 20)]
    public void Page_size_is_limited_to_20_50_100(string query, int expected) =>
        Assert.Equal(expected, Parse(query).PageSize);

    [Fact]
    public void Typed_filter_values_are_parsed_and_bad_values_ignored()
    {
        var r = Parse("?price_from=1000&price_to=abc&created_from=2026-10-01&q=%20iphone%20");

        Assert.Equal(1000m, r.Decimal("price_from"));
        Assert.Null(r.Decimal("price_to"));
        Assert.Equal(new DateOnly(2026, 10, 1), r.Date("created_from"));
        Assert.Equal("iphone", r.Text("q"));
    }

    [Fact]
    public void Url_helpers_keep_filters_and_reset_page()
    {
        var r = Parse("?q=iphone&page=3&sort=price");

        Assert.Equal("/admin/products?q=iphone&sort=price&dir=desc", r.WithSort("price"));
        Assert.Equal("/admin/products?q=iphone&page=2&sort=price", r.WithPage(2));
        Assert.Equal("/admin/products?sort=price", r.Without("q")); // a filter change goes back to page 1
    }
}
