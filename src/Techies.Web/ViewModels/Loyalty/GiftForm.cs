using System.ComponentModel.DataAnnotations;

namespace Techies.Web.ViewModels.Loyalty;

public sealed class GiftForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên quà.")]
    [StringLength(160)]
    [Display(Name = "Tên quà")]
    public string Name { get; set; } = "";

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    [Display(Name = "Hình ảnh")]
    public IFormFile? Image { get; set; }
    public string? CurrentImageUrl { get; set; }

    [Display(Name = "Điểm cần đổi")]
    public long PointsCost { get; set; }

    [Display(Name = "Hạng tối thiểu")]
    public short MinTier { get; set; }

    [Display(Name = "Số lượng quà có sẵn")]
    public int StockAvailable { get; set; }

    [Display(Name = "Đang mở đổi quà")]
    public bool Active { get; set; } = true;
}
