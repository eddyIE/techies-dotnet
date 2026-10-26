using Techies.Web.Domain;

namespace Techies.Web.Infrastructure.Grid;

public sealed record GridColumn(string Label, string? SortKey = null, string? CssClass = null);

public enum GridFilterType { Text, Select, NumberRange, DateRange }

public sealed record GridOption(string Value, string Label);

// Range filters use the query keys "<Key>_from" and "<Key>_to".
public sealed record GridFilter(string Key, string Label, GridFilterType Type,
    IReadOnlyList<GridOption>? Options = null, bool Money = false);

// A mass action posts the selected row ids ("ids") to Url.
public sealed record GridMassAction(string Label, string Url, string? Confirm = null);

public sealed record GridRowAction(string Label, string Url, bool Post = false, string? Confirm = null,
    bool Danger = false, bool NewTab = false);

public sealed record GridChip(string Text, string RemoveUrl);

// Everything the shared _Grid partial needs. Rows are rendered by a grid-specific partial.
public sealed class GridModel
{
    public required string Id { get; init; }
    public required GridRequest Request { get; init; }
    public required IReadOnlyList<GridColumn> Columns { get; init; }
    public IReadOnlyList<GridFilter> Filters { get; init; } = [];
    public IReadOnlyList<GridMassAction> MassActions { get; init; } = [];
    public string KeywordPlaceholder { get; init; } = "Tìm kiếm...";
    public required string RowsPartial { get; init; }
    public required object Rows { get; init; }
    public required int Total { get; init; }
    public string EmptyText { get; init; } = "Không có dữ liệu phù hợp.";

    public bool Selectable => MassActions.Count > 0;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)Request.PageSize));
    public int ColumnCount => Columns.Count + (Selectable ? 1 : 0) + 1;

    public IEnumerable<GridChip> Chips()
    {
        if (Request.Text("q") is { } q) yield return new($"Từ khóa: {q}", Request.Without("q"));
        foreach (var f in Filters)
        {
            if (f.Type is GridFilterType.NumberRange or GridFilterType.DateRange)
            {
                var from = Request.Text(f.Key + "_from");
                var to = Request.Text(f.Key + "_to");
                if (from is null && to is null) continue;
                yield return new($"{f.Label}: {Show(f, from) ?? "…"} – {Show(f, to) ?? "…"}", Request.Without(f.Key + "_from", f.Key + "_to"));
            }
            else if (Request.Text(f.Key) is { } value)
            {
                var label = f.Options?.FirstOrDefault(o => o.Value.Equals(value, StringComparison.OrdinalIgnoreCase))?.Label ?? value;
                yield return new($"{f.Label}: {label}", Request.Without(f.Key));
            }
        }
    }

    private static string? Show(GridFilter f, string? value) => value is null ? null
        : f.Money && decimal.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var d) ? Format.Money(d)
        : f.Type == GridFilterType.DateRange && DateOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var date) ? date.ToString("dd/MM/yyyy")
        : value;
}

public sealed record BulkResult(int Done, int Total, IReadOnlyList<string> Errors)
{
    public string Summary(string verb)
    {
        var text = $"Đã {verb} {Done} / {Total}.";
        if (Errors.Count > 0) text += " Bỏ qua: " + string.Join("; ", Errors.Distinct().Take(3)) + (Errors.Distinct().Count() > 3 ? "…" : "");
        return text;
    }
}

public static class Bulk
{
    // Applies one action to each selected row; a rule violation skips that row, not the batch.
    public static async Task<BulkResult> RunAsync<TId>(IReadOnlyCollection<TId> ids, Func<TId, Task> action)
    {
        var done = 0;
        var errors = new List<string>();
        foreach (var id in ids.Distinct())
        {
            try
            {
                await action(id);
                done++;
            }
            catch (DomainException ex)
            {
                errors.Add(ex.Message);
            }
        }
        return new BulkResult(done, ids.Distinct().Count(), errors);
    }
}
