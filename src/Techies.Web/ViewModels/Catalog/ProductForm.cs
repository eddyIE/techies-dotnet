using System.ComponentModel.DataAnnotations;

namespace Techies.Web.ViewModels.Catalog;

public sealed class ProductForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
    [StringLength(200, ErrorMessage = "Tên tối đa 200 ký tự.")]
    [Display(Name = "Tên sản phẩm")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
    [Display(Name = "Danh mục")]
    public Guid? CategoryId { get; set; }

    [Range(1, 1_000_000_000, ErrorMessage = "Giá bán phải lớn hơn 0.")]
    [Display(Name = "Giá bán (VNĐ)")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
    [Display(Name = "Mô tả")]
    public string Description { get; set; } = "";

    [Display(Name = "Đang kinh doanh")]
    public bool Active { get; set; } = true;

    [Display(Name = "Ảnh đại diện")]
    public IFormFile? Thumbnail { get; set; }
    public string? CurrentThumbnailUrl { get; set; }

    [Display(Name = "Thêm ảnh vào thư viện")]
    public List<IFormFile> NewImages { get; set; } = [];

    // Gallery images kept, in display order (the form can remove and reorder them).
    public List<string> ExistingImages { get; set; } = [];

    public List<SpecInput> Specs { get; set; } = [];
}

public sealed class SpecInput
{
    public string? Name { get; set; }
    public string? Value { get; set; }
}
