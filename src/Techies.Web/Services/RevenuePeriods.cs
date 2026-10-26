using Techies.Web.Infrastructure;

namespace Techies.Web.Services;

public enum RevenuePeriod { Last7Days, ThisMonth, ThisQuarter, ThisYear }

public sealed record RevenueBucket(string Label, DateTimeOffset Start, DateTimeOffset End);

public sealed record PeriodRange(DateTimeOffset Start, DateTimeOffset End, DateTimeOffset PreviousStart,
    DateTimeOffset PreviousEnd, List<RevenueBucket> Buckets);

// Dashboard time ranges (BRD §1), in Vietnam local time. The comparison span is the same
// elapsed length at the start of the previous period: 1–26 Oct is compared with 1–26 Sep.
public static class RevenuePeriods
{
    public static PeriodRange For(RevenuePeriod period, DateTimeOffset now)
    {
        var local = Format.ToLocal(now);
        var today = new DateTimeOffset(local.Date, local.Offset);
        var start = period switch
        {
            RevenuePeriod.Last7Days => today.AddDays(-6),
            RevenuePeriod.ThisMonth => new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, local.Offset),
            RevenuePeriod.ThisQuarter => new DateTimeOffset(local.Year, (local.Month - 1) / 3 * 3 + 1, 1, 0, 0, 0, local.Offset),
            _ => new DateTimeOffset(local.Year, 1, 1, 0, 0, 0, local.Offset),
        };
        var previousStart = period switch
        {
            RevenuePeriod.Last7Days => start.AddDays(-7),
            RevenuePeriod.ThisMonth => start.AddMonths(-1),
            RevenuePeriod.ThisQuarter => start.AddMonths(-3),
            _ => start.AddYears(-1),
        };
        var previousEnd = previousStart + (now - start);
        if (previousEnd > start) previousEnd = start;

        // Boundaries are computed in local time (labels, month starts) but handed out in UTC,
        // which is what timestamptz parameters require.
        return new PeriodRange(start.ToUniversalTime(), now.ToUniversalTime(), previousStart.ToUniversalTime(),
            previousEnd.ToUniversalTime(), Buckets(period, start, now));
    }

    public static double? PercentChange(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((double)((current - previous) / previous * 100), 1);

    private static List<RevenueBucket> Buckets(RevenuePeriod period, DateTimeOffset start, DateTimeOffset end)
    {
        var buckets = new List<RevenueBucket>();
        var cursor = start;
        var week = 1;
        while (cursor < end)
        {
            var next = period switch
            {
                RevenuePeriod.ThisQuarter => cursor.AddDays(7),
                RevenuePeriod.ThisYear => cursor.AddMonths(1),
                _ => cursor.AddDays(1),
            };
            var label = period switch
            {
                RevenuePeriod.ThisQuarter => $"Tuần {week++}",
                RevenuePeriod.ThisYear => $"T{cursor.Month}",
                _ => cursor.ToString("dd/MM"),
            };
            buckets.Add(new RevenueBucket(label, cursor.ToUniversalTime(), (next < end ? next : end).ToUniversalTime()));
            cursor = next;
        }
        return buckets;
    }
}
