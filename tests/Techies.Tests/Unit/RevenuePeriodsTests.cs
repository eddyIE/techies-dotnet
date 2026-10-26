using Techies.Web.Services;

namespace Techies.Tests.Unit;

public class RevenuePeriodsTests
{
    // 26/10/2026 14:05 in Vietnam (UTC+7).
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 7, 5, 0, TimeSpan.Zero);
    private static DateTimeOffset VnMidnight(int y, int m, int d) => new DateTimeOffset(y, m, d, 0, 0, 0, TimeSpan.FromHours(7));

    [Fact]
    public void Last_7_days_has_daily_buckets_ending_today()
    {
        var p = RevenuePeriods.For(RevenuePeriod.Last7Days, Now);

        Assert.Equal(VnMidnight(2026, 10, 20), p.Start);
        Assert.Equal(Now, p.End);
        Assert.Equal(["20/10", "21/10", "22/10", "23/10", "24/10", "25/10", "26/10"], p.Buckets.Select(b => b.Label));
        Assert.Equal(VnMidnight(2026, 10, 13), p.PreviousStart);
        Assert.Equal(Now.AddDays(-7), p.PreviousEnd);
    }

    [Fact]
    public void This_month_compares_with_same_elapsed_span_of_last_month()
    {
        var p = RevenuePeriods.For(RevenuePeriod.ThisMonth, Now);

        Assert.Equal(VnMidnight(2026, 10, 1), p.Start);
        Assert.Equal(26, p.Buckets.Count);
        Assert.Equal(VnMidnight(2026, 9, 1), p.PreviousStart);
        Assert.Equal(VnMidnight(2026, 9, 1) + (Now - VnMidnight(2026, 10, 1)), p.PreviousEnd);
    }

    [Fact]
    public void This_quarter_has_weekly_buckets()
    {
        var p = RevenuePeriods.For(RevenuePeriod.ThisQuarter, Now);

        Assert.Equal(VnMidnight(2026, 10, 1), p.Start);
        Assert.Equal(["Tuần 1", "Tuần 2", "Tuần 3", "Tuần 4"], p.Buckets.Select(b => b.Label));
        Assert.Equal(VnMidnight(2026, 7, 1), p.PreviousStart);
    }

    [Fact]
    public void This_year_has_monthly_buckets()
    {
        var p = RevenuePeriods.For(RevenuePeriod.ThisYear, Now);

        Assert.Equal(VnMidnight(2026, 1, 1), p.Start);
        Assert.Equal(Enumerable.Range(1, 10).Select(m => $"T{m}"), p.Buckets.Select(b => b.Label));
        Assert.Equal(VnMidnight(2025, 1, 1), p.PreviousStart);
    }

    [Fact]
    public void Previous_span_never_runs_into_the_current_period()
    {
        // 31 March: "this month" has 31 days elapsed but February only had 28.
        var march31 = new DateTimeOffset(2026, 3, 31, 10, 0, 0, TimeSpan.FromHours(7));

        var p = RevenuePeriods.For(RevenuePeriod.ThisMonth, march31);

        Assert.Equal(VnMidnight(2026, 3, 1), p.PreviousEnd);
    }

    [Theory]
    [InlineData(150, 100, 50.0)]
    [InlineData(50, 100, -50.0)]
    [InlineData(100, 300, -66.7)]
    public void Percent_change_against_previous(decimal current, decimal previous, double expected) =>
        Assert.Equal(expected, RevenuePeriods.PercentChange(current, previous));

    [Fact]
    public void Percent_change_is_undefined_when_previous_is_zero() =>
        Assert.Null(RevenuePeriods.PercentChange(10, 0));
}
