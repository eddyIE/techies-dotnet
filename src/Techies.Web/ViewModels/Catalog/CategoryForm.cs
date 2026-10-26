using System.ComponentModel.DataAnnotations;

namespace Techies.Web.ViewModels.Catalog;

public sealed class CategoryForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
    [StringLength(120, ErrorMessage = "Tên tối đa 120 ký tự.")]
    [Display(Name = "Tên danh mục")]
    public string Name { get; set; } = "";

    [Range(0, 1000, ErrorMessage = "Thứ tự từ 0 đến 1000.")]
    [Display(Name = "Thứ tự hiển thị")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Ảnh icon")]
    public IFormFile? Icon { get; set; }

    public string? CurrentImageUrl { get; set; }
}
