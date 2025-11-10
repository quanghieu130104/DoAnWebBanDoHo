using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DoAnWebBanDoHo.Areas.Identity.Data; // Assuming ApplicationUser is here

namespace DoAnWebBanDoHo.Models
{
    public class ProductReview
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; } // Khóa ngoại đến Product
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Lưu UserId nếu muốn chỉ user đăng nhập mới đánh giá
        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá.")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1 đến 5 sao.")]
        public int Rating { get; set; } // 1 to 5 stars

        [StringLength(1000, ErrorMessage = "Bình luận không quá 1000 ký tự.")]
        [Display(Name = "Bình luận")]
        public string? Comment { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên của bạn.")] // Hoặc lấy từ User.Identity.Name nếu bắt buộc đăng nhập
        [StringLength(100)]
        [Display(Name = "Tên người đánh giá")]
        public string ReviewerName { get; set; }

        public DateTime ReviewDate { get; set; } = DateTime.Now;

        public bool IsApproved { get; set; } = true; // Thêm nếu muốn Admin duyệt bình luận
    }
}