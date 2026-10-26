using System.ComponentModel.DataAnnotations;

namespace Techies.Web.ViewModels.Staff;

public sealed class RoleForm
{
    [Required(ErrorMessage = "Vui lòng nhập tên vai trò.")]
    [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự.")]
    [Display(Name = "Tên vai trò")]
    public string Name { get; set; } = "";

    [Display(Name = "Mô tả")]
    public string? Description { get; set; }

    // Permission codes from the catalogue (Infrastructure.Permissions).
    public List<string> Permissions { get; set; } = [];
}

public sealed record RoleRow(int Id, string Name, string? Description, bool IsSystem, int StaffCount, int PermissionCount);
