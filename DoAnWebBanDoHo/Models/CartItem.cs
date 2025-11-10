using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DoAnWebBanDoHo.Models
{
    public class CartItem
    {
        // Product ID
        [Key]
        public int ProductId { get; set; }

        // Product Details
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountedPrice { get; set; } // Giá khuyến mãi nếu có
        public string? ImageUrl { get; set; }

        // Quantity in cart
        [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000.")]
        public int Quantity { get; set; }

        // Navigation property to Product (Optional, but useful for full product data)
        // [ForeignKey("ProductId")] // Could be used if CartItem was a DB entity
        // public Product Product { get; set; }

        // Calculated property for total price of this item
        [NotMapped] // Tell EF Core not to map this to a database column
        public decimal TotalPrice => (DiscountedPrice.HasValue ? DiscountedPrice.Value : Price) * Quantity;
    }
}
