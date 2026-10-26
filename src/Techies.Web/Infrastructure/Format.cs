using System.Globalization;

namespace Techies.Web.Infrastructure;

// Display formatting for Vietnamese users. The request culture stays invariant so that form
// numbers bind reliably; views call these helpers explicitly instead.
public static class Format
{
    public static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    public static readonly TimeZoneInfo VietnamTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public static string Money(decimal amount) =>
        Math.Round(amount, 0, MidpointRounding.AwayFromZero).ToString("#,0", Vietnamese) + "đ";

    public static string LocalTime(DateTimeOffset utc) =>
        ToLocal(utc).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string LocalDate(DateTimeOffset utc) =>
        ToLocal(utc).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public static DateTimeOffset ToLocal(DateTimeOffset utc) => TimeZoneInfo.ConvertTime(utc, VietnamTime);

    // <input type="datetime-local"> value (Vietnam wall-clock time) -> UTC instant.
    public static DateTimeOffset? FromLocalInput(DateTime? local) =>
        local is null ? null
        : new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local.Value, DateTimeKind.Unspecified), VietnamTime));

    public static DateTime? ToLocalInput(DateTimeOffset? utc) => utc is null ? null : ToLocal(utc.Value).DateTime;
}
