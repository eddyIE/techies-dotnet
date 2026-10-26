using System.Globalization;
using Microsoft.AspNetCore.WebUtilities;

namespace Techies.Web.Infrastructure.Grid;

// The state of an admin grid, read from (and written back to) the query string, so every grid
// view is a bookmarkable URL: ?q=…&category=…&price_from=…&sort=price&dir=desc&page=2&size=50
public sealed class GridRequest
{
    public static readonly int[] PageSizes = [20, 50, 100];

    private readonly PathString _path;
    private readonly List<KeyValuePair<string, string>> _query;

    private GridRequest(PathString path, List<KeyValuePair<string, string>> query, string sort, bool desc, int page, int pageSize)
    {
        _path = path;
        _query = query;
        Sort = sort;
        Desc = desc;
        Page = page;
        PageSize = pageSize;
    }

    public string Sort { get; }
    public bool Desc { get; }
    public int Page { get; }
    public int PageSize { get; }
    public string Url => Build(_query);
    public bool HasExplicitSort => _query.Any(kv => kv.Key == "sort");

    // Only whitelisted sort keys are honoured; anything else falls back to the default.
    public static GridRequest From(PathString path, QueryString queryString, IReadOnlyCollection<string> sortable,
        string defaultSort, bool defaultDesc)
    {
        var query = QueryHelpers.ParseQuery(queryString.Value)
            .SelectMany(kv => kv.Value.Select(v => new KeyValuePair<string, string>(kv.Key, v ?? "")))
            .Where(kv => kv.Value.Length > 0 && kv.Key != "__RequestVerificationToken")
            .ToList();
        string? Get(string key) => query.LastOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value;

        var sort = Get("sort");
        var known = sort is not null && sortable.Contains(sort);
        var desc = known ? Get("dir") == "desc" : defaultDesc;
        var page = int.TryParse(Get("page"), out var p) && p > 0 ? p : 1;
        var size = int.TryParse(Get("size"), out var s) && PageSizes.Contains(s) ? s : PageSizes[0];
        return new GridRequest(path, query, known ? sort! : defaultSort, desc, page, size);
    }

    public string? Text(string key)
    {
        var value = _query.LastOrDefault(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public IReadOnlyList<string> All(string key) =>
        _query.Where(kv => kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)).Select(kv => kv.Value).ToList();

    public decimal? Decimal(string key) =>
        decimal.TryParse(Text(key), NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;

    public int? Int(string key) => int.TryParse(Text(key), out var i) ? i : null;

    public bool? Bool(string key) => bool.TryParse(Text(key), out var b) ? b : null;

    public DateOnly? Date(string key) =>
        DateOnly.TryParseExact(Text(key), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    public TEnum? Enum<TEnum>(string key) where TEnum : struct, System.Enum =>
        System.Enum.TryParse<TEnum>(Text(key), ignoreCase: true, out var e) && System.Enum.IsDefined(e) ? e : null;

    // Clicking the current sort column flips the direction; another column starts ascending.
    public string WithSort(string key)
    {
        var desc = key == Sort && !Desc;
        return Build(Replace(Replace(_query, "page", null), "sort", key), desc ? ("dir", "desc") : null, removeDir: true);
    }

    public string WithPage(int page) => Build(Replace(_query, "page", page.ToString(CultureInfo.InvariantCulture)));

    public string WithPageSize(int size) => Build(Replace(Replace(_query, "page", null), "size", size.ToString(CultureInfo.InvariantCulture)));

    // Removing a filter (chip ✕) goes back to page 1.
    public string Without(params string[] keys) =>
        Build(_query.Where(kv => !keys.Contains(kv.Key, StringComparer.OrdinalIgnoreCase) && kv.Key != "page").ToList());

    // Keep only sort and page size ("Xóa tất cả" filters).
    public string Cleared() => Build(_query.Where(kv => kv.Key is "sort" or "dir" or "size").ToList());

    // Every filter key currently set, for hidden inputs that carry state across forms.
    public IEnumerable<KeyValuePair<string, string>> Pairs => _query;

    private static List<KeyValuePair<string, string>> Replace(List<KeyValuePair<string, string>> query, string key, string? value)
    {
        var result = new List<KeyValuePair<string, string>>();
        var replaced = false;
        foreach (var kv in query)
        {
            if (!kv.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) { result.Add(kv); continue; }
            if (!replaced && value is not null) result.Add(new(key, value));
            replaced = true;
        }
        if (!replaced && value is not null) result.Add(new(key, value));
        return result;
    }

    private string Build(List<KeyValuePair<string, string>> query, (string Key, string Value)? extra = null, bool removeDir = false)
    {
        var pairs = query.Where(kv => !(removeDir && kv.Key == "dir")).ToList();
        if (extra is { } e) pairs.Add(new(e.Key, e.Value));
        return pairs.Count == 0 ? _path.Value! : _path + QueryString.Create(pairs!).Value;
    }
}
