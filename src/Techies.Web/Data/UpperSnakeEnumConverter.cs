using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Techies.Web.Data;

// SuperAdmin <-> "SUPER_ADMIN", AwaitingPayment <-> "AWAITING_PAYMENT".
public sealed partial class UpperSnakeEnumConverter<TEnum>() : ValueConverter<TEnum, string>(
    v => ToDb(v),
    v => FromDb(v))
    where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> ByName =
        Enum.GetValues<TEnum>().ToDictionary(e => ToDb(e));

    private static string ToDb(TEnum value) => WordBoundary().Replace(value.ToString(), "_$1").ToUpperInvariant();

    private static TEnum FromDb(string value) => ByName[value];

    [GeneratedRegex("(?<!^)([A-Z])")]
    private static partial Regex WordBoundary();
}
