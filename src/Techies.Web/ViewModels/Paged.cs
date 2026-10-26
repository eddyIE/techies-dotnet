namespace Techies.Web.ViewModels;

public sealed record Paged<T>(List<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
}
