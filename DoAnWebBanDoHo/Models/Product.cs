using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic; // <<<< Thêm cái này để dùng ICollection/List

namespace DoAnWebBanDoHo.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200)]
        [Display(Name = "Tên Sản Phẩm")]
        public string Name { get; set; }

        [Display(Name = "Mô Tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá không được để trống.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0.")]
        [Display(Name = "Giá Gốc")] // << Đổi tên: Đây là giá bị gạch
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Giá Khuyến Mãi")] // << Đây là giá bán màu đỏ
        [Range(0.00, double.MaxValue, ErrorMessage = "Giá khuyến mãi phải lớn hơn hoặc bằng 0.")]
        public decimal? DiscountedPrice { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho không được để trống.")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0.")]
        [Display(Name = "Số Lượng Tồn Kho")]
        public int StockQuantity { get; set; }

        [StringLength(500)]
        [Display(Name = "URL Ảnh Chính")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục.")]
        [Display(Name = "Danh Mục")]
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }

        [Display(Name = "Ngày Tạo")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Lần Cập Nhật Cuối")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        // ===== Xử lý Upload Ảnh Chính (Bạn đã làm) =====
        [NotMapped]
        [Display(Name = "Chọn Ảnh Chính")]
        public IFormFile? ImageFile { get; set; }

        // ===== BỔ SUNG CÁC THUỘC TÍNH TỪ ẢNH MẪU =====
        public virtual ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();

        [StringLength(100)]
        [Display(Name = "Mã SP (SKU)")]
        public string? SKU { get; set; }

        [StringLength(50)]
        [Display(Name = "Giới tính")]
        public string? Gender { get; set; }

        [StringLength(200)]
        [Display(Name = "Chất liệu")]
        public string? Material { get; set; }

        [StringLength(100)]
        [Display(Name = "Xuất xứ")]
        public string? Origin { get; set; }

        // ===== BỔ SUNG CHO GALLERY ẢNH (Câu hỏi trước) =====

        [NotMapped]
        [Display(Name = "Chọn Ảnh Gallery (Nhiều ảnh)")]
        public List<IFormFile>? GalleryFiles { get; set; }

        public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
    }
}