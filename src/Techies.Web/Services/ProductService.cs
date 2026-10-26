using Microsoft.EntityFrameworkCore;
using Techies.Web.Data;
using Techies.Web.Domain;
using Techies.Web.Infrastructure;
using Techies.Web.ViewModels;
using Techies.Web.ViewModels.Catalog;

namespace Techies.Web.Services;

public sealed class ProductService(AppDbContext db)
{
    public Task<Paged<ProductRow>> SearchAsync(ProductFilter filter, CancellationToken ct = default)
    {
        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Q))
            query = query.Where(p => EF.Functions.ILike(p.Name, QueryExtensions.Contains(filter.Q)));
        if (filter.CategoryId is { } categoryId)
            query = query.Where(p => p.CategoryId == categoryId);
        if (filter.OutOfStock)
            query = query.Where(p => p.Stock!.Available == 0);
        if (filter.Active is { } active)
            query = query.Where(p => p.Active == active);

        return query
            .OrderBy(p => p.Name)
            .Select(p => new ProductRow(p.Id, p.Name, p.Category.Name, p.Price, p.Stock!.Available, p.Active, p.ThumbnailUrl))
            .ToPagedAsync(filter.Page, ct: ct);
    }

    public async Task<Product> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Products.Include(p => p.Images).Include(p => p.Specs).SingleOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new DomainException("Không tìm thấy sản phẩm.");

    public async Task<Guid> CreateAsync(ProductForm form, string? thumbnailUrl, IReadOnlyList<string> newImageUrls,
        CancellationToken ct = default)
    {
        Validate(form);
        var product = new Product
        {
            Slug = await UniqueSlugAsync(form.Name, null, ct),
            ThumbnailUrl = thumbnailUrl,
            CreatedAt = DateTimeOffset.UtcNow,
            // New products start with no stock; staff add it through "Nhập hàng".
            Stock = new StockItem { Available = 0, UpdatedAt = DateTimeOffset.UtcNow },
        };
        Apply(product, form, newImageUrls);
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task UpdateAsync(Guid id, ProductForm form, string? thumbnailUrl, IReadOnlyList<string> newImageUrls,
        CancellationToken ct = default)
    {
        Validate(form);
        var product = await GetAsync(id, ct);
        if (product.Name != form.Name.Trim())
            product.Slug = await UniqueSlugAsync(form.Name, id, ct);
        if (thumbnailUrl is not null) product.ThumbnailUrl = thumbnailUrl;
        db.ProductImages.RemoveRange(product.Images);
        db.ProductSpecs.RemoveRange(product.Specs);
        Apply(product, form, newImageUrls);
        await db.SaveChangesAsync(ct);
    }

    private static void Validate(ProductForm form)
    {
        if (form.Price <= 0) throw new DomainException("Giá bán phải lớn hơn 0.");
        if (form.CategoryId is null) throw new DomainException("Vui lòng chọn danh mục.");
    }

    // Images and specs are replaced wholesale by what the form submitted, in its order.
    private void Apply(Product product, ProductForm form, IReadOnlyList<string> newImageUrls)
    {
        product.Name = form.Name.Trim();
        product.CategoryId = form.CategoryId!.Value;
        product.Price = form.Price;
        product.Description = form.Description.Trim();
        product.Active = form.Active;
        product.Images = form.ExistingImages.Concat(newImageUrls)
            .Select((url, i) => new ProductImage { ProductId = product.Id, Url = url, DisplayOrder = i }).ToList();
        product.Specs = form.Specs
            .Where(s => !string.IsNullOrWhiteSpace(s.Name) && !string.IsNullOrWhiteSpace(s.Value))
            .Select((s, i) => new ProductSpec { ProductId = product.Id, SpecName = s.Name!.Trim(), SpecValue = s.Value!.Trim(), DisplayOrder = i })
            .ToList();
        db.ProductImages.AddRange(product.Images);
        db.ProductSpecs.AddRange(product.Specs);
    }

    private async Task<string> UniqueSlugAsync(string name, Guid? exceptId, CancellationToken ct)
    {
        var baseSlug = Slug.From(name);
        var taken = await db.Products
            .Where(p => p.Id != exceptId && (p.Slug == baseSlug || p.Slug.StartsWith(baseSlug + "-")))
            .Select(p => p.Slug).ToListAsync(ct);
        return Slugs.FirstFree(baseSlug, taken);
    }
}
