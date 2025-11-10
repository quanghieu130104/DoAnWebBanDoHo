using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWebBanDoHo.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; } // Khóa ngoại liên kết với Order
        [ForeignKey("OrderId")]
        public Order? Order { get; set; } // Navigation property đến Order

        [Required]
        public int ProductId { get; set; } // ID của sản phẩm trong đơn hàng
        [ForeignKey("ProductId")]
        public Product? Product { get; set; } // Navigation property đến Product

        [Required(ErrorMessage = "Tên sản phẩm không được để trống.")]
        [StringLength(200)]
        [Display(Name = "Tên Sản Phẩm")]
        public string ProductName { get; set; } // Lưu lại tên sản phẩm tại thời điểm đặt hàng

        [Required(ErrorMessage = "Giá sản phẩm không được để trống.")]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Giá")]
        public decimal Price { get; set; } // Giá sản phẩm tại thời điểm đặt hàng

        [Display(Name = "Giá Khuyến Mãi")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? DiscountedPrice { get; set; } // Giá khuyến mãi tại thời điểm đặt hàng (nếu có)

        [Required(ErrorMessage = "Số lượng không được để trống.")]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
        [Display(Name = "Số Lượng")]
        public int Quantity { get; set; }

        // Thuộc tính tính toán để có được tổng giá trị cho mục này trong đơn hàng
        [NotMapped]
        public decimal TotalItemPrice => (DiscountedPrice.HasValue ? DiscountedPrice.Value : Price) * Quantity;
    }
}
