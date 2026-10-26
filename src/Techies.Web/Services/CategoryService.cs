using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.Infrastructure.Grid;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Services;

public sealed record CategoryRow(Guid Id, string Name, string Slug, string? ImageUrl, int DisplayOrder, int ProductCount);

public sealed class CategoryService(AppDbContext db)
{
    public static readonly string[] GridSorts = ["order", "name", "products"];

    public async Task<(List<CategoryRow> Rows, int Total)> GridAsync(GridRequest r, CancellationToken ct = default)
    {
        var query = db.Categories.AsNoTracking();
        if (r.Text("q") is { } q) query = query.Where(c => EF.Functions.ILike(c.Name, QueryExtensions.Contains(q)));
        query = (r.Sort, r.Desc) switch
        {
            ("name", false) => query.OrderBy(c => c.Name),
            ("name", true) => query.OrderByDescending(c => c.Name),
            ("products", false) => query.OrderBy(c => db.Products.Count(p => p.CategoryId == c.Id)).ThenBy(c => c.Name),
            ("products", true) => query.OrderByDescending(c => db.Products.Count(p => p.CategoryId == c.Id)).ThenBy(c => c.Name),
            (_, true) => query.OrderByDescending(c => c.DisplayOrder).ThenBy(c => c.Name),
            _ => query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name),
        };
        var rows = query.Select(c => new CategoryRow(c.Id, c.Name, c.Slug, c.ImageUrl, c.DisplayOrder, db.Products.Count(p => p.CategoryId == c.Id)));
        return (await rows.Skip((r.Page - 1) * r.PageSize).Take(r.PageSize).ToListAsync(ct), await query.CountAsync(ct));
    }

    public Task<List<Category>> ListAsync(CancellationToken ct = default) =>
        db.Categories.AsNoTracking().OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync(ct);

    public async Task<Category> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Categories.FindAsync([id], ct) ?? throw new DomainException("Không tìm thấy danh mục.");

    public async Task<Category> CreateAsync(CategoryForm form, string? iconUrl, CancellationToken ct = default)
    {
        var category = new Category
        {
            Name = form.Name.Trim(), DisplayOrder = form.DisplayOrder, ImageUrl = iconUrl,
            Slug = await UniqueSlugAsync(form.Name, null, ct),
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateAsync(Guid id, CategoryForm form, string? iconUrl, CancellationToken ct = default)
    {
        var category = await GetAsync(id, ct);
        if (category.Name != form.Name.Trim())
            category.Slug = await UniqueSlugAsync(form.Name, id, ct);
        category.Name = form.Name.Trim();
        category.DisplayOrder = form.DisplayOrder;
        if (iconUrl is not null) category.ImageUrl = iconUrl;
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> UniqueSlugAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        var baseSlug = Slug.From(name);
        var taken = await db.Categories
            .Where(c => c.Id != exceptId && (c.Slug == baseSlug || c.Slug.StartsWith(baseSlug + "-")))
            .Select(c => c.Slug).ToListAsync(ct);
        return Slugs.FirstFree(baseSlug, taken);
    }
}
