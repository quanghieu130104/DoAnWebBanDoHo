using DoAnWebBanDoHo.Data;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DoAnWebBanDoHo.Areas.Identity.Data;
namespace DoAnWebBanDoHo.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        // ==== USER ====
       
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        // ==== Thông tin giao hàng ====
        [Required(ErrorMessage = "Tên người nhận không được để trống.")]
        [StringLength(100)]
        [Display(Name = "Tên Người Nhận")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        [Display(Name = "Số Điện Thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống.")]
        [StringLength(500)]
        [Display(Name = "Địa Chỉ Nhận Hàng")]
        public string ShippingAddress { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        // ==== Thông tin đơn hàng ====
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Tổng Tiền")]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Ngày Đặt Hàng")]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        [Display(Name = "Trạng Thái Đơn Hàng")]
        public string OrderStatus { get; set; } = "Chờ xác nhận";

        [StringLength(1000)]
        [Display(Name = "Ghi Chú")]
        public string? Notes { get; set; }

        [StringLength(50)]
        [Display(Name = "Phương Thức Thanh Toán")]
        public string PaymentMethod { get; set; } = "Thanh toán khi nhận hàng (COD)";

        // ==== Navigation ====
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
