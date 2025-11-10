using System.ComponentModel.DataAnnotations; // Thêm namespace này cho Data Annotations

namespace DoAnWebBanDoHo.Models // Đảm bảo namespace này khớp với project của bạn
{
    public class Category
    {
        [Key] // Đánh dấu Id là khóa chính
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống.")] // Bắt buộc nhập tên
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")] // Giới hạn độ dài
        [Display(Name = "Tên Danh Mục")] // Tên hiển thị trong UI
        public string Name { get; set; }

        [Display(Name = "Mô Tả")] // Tên hiển thị trong UI
        public string? Description { get; set; } // Có thể null
    }
}
