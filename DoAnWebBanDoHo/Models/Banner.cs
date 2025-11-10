using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http; // Cho IFormFile

namespace DoAnWebBanDoHo.Models
{
    public class Banner
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng cung cấp URL ảnh.")]
        [StringLength(500)]
        [Display(Name = "URL Ảnh Banner")]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        [Display(Name = "Tiêu đề (Tùy chọn)")]
        public string? Title { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả (Tùy chọn)")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Link liên kết (Tùy chọn)")]
        public string? LinkUrl { get; set; } // Nếu muốn banner có thể click vào

        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0; // Để sắp xếp banner

        [Display(Name = "Đang hoạt động")]
        public bool IsActive { get; set; } = true; // Để ẩn/hiện banner

        // Dùng để upload file ảnh từ Admin
        [NotMapped]
        [Display(Name = "Chọn ảnh mới")]
        public IFormFile? ImageFile { get; set; }
    }
}