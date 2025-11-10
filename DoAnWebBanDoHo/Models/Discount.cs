using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWebBanDoHo.Models // Đảm bảo namespace này khớp với project của bạn
{
    public class Discount
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Mã giảm giá không được để trống.")]
        [StringLength(50, ErrorMessage = "Mã giảm giá không được vượt quá 50 ký tự.")]
        [Display(Name = "Mã Giảm Giá")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Loại giảm giá không được để trống.")]
        [StringLength(20)]
        [Display(Name = "Loại Giảm Giá")] // Ví dụ: "Percentage" (%), "FixedAmount" (Giá trị cố định)
        public string DiscountType { get; set; }

        [Required(ErrorMessage = "Giá trị giảm giá không được để trống.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0.")]
        [Display(Name = "Giá Trị")] // Ví dụ: 10 (cho 10%) hoặc 50000 (cho 50.000 VNĐ)
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Đơn Hàng Tối Thiểu")] // Áp dụng nếu tổng đơn hàng đạt giá trị này
        [Range(0.00, double.MaxValue, ErrorMessage = "Giá trị đơn hàng tối thiểu phải lớn hơn hoặc bằng 0.")]
        public decimal? MinimumOrderAmount { get; set; }

        [Display(Name = "Ngày Bắt Đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày Kết Thúc")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Kích Hoạt")]
        public bool IsActive { get; set; } = true; // Mặc định là kích hoạt

        [Display(Name = "Giới Hạn Sử Dụng")] // Tổng số lần mã này có thể được sử dụng
        [Range(0, int.MaxValue, ErrorMessage = "Giới hạn sử dụng phải lớn hơn hoặc bằng 0.")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Số Lần Đã Sử Dụng")]
        public int UsedCount { get; set; } = 0; // Số lần mã đã được sử dụng

        [Display(Name = "Áp dụng cho")]
        [StringLength(50)]
        public string AppliesTo { get; set; } = "Order"; // "Order" hoặc "Product" (nếu muốn phức tạp hơn)

        // Bạn có thể thêm các thuộc tính khác như:
        // public bool IsOneTimeUse { get; set; } // Mã chỉ dùng được 1 lần cho mỗi người dùng
        // public string? ProductIds { get; set; } // Nếu áp dụng cho sản phẩm cụ thể (lưu dưới dạng JSON/CSV)
    }
}
