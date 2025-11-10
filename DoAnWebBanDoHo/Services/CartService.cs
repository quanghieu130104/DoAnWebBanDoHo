using DoAnWebBanDoHo.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json; // For JSON serialization/deserialization
using DoAnWebBanDoHo.Data; // Cần cho ApplicationDbContext để truy cập Discount
using Microsoft.EntityFrameworkCore; // Cần cho ToListAsync, FindAsync, v.v.

namespace DoAnWebBanDoHo.Services
{
    public class CartService
    {
        private readonly ISession _session;
        private readonly ApplicationDbContext _context; // Inject DbContext để truy cập Discount
        private const string CartSessionKey = "ShoppingCart";
        private const string DiscountSessionKey = "AppliedDiscount"; // Key để lưu mã giảm giá đã áp dụng

        public CartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context) // Inject DbContext
        {
            _session = httpContextAccessor.HttpContext.Session;
            _context = context; // Gán DbContext
        }

        // Lấy giỏ hàng từ session
        public List<CartItem> GetCartItems()
        {
            var cartJson = _session.GetString(CartSessionKey);
            return cartJson == null
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        // Lưu giỏ hàng vào session
        private void SaveCartItems(List<CartItem> cartItems)
        {
            _session.SetString(CartSessionKey, JsonSerializer.Serialize(cartItems));
        }

        // Lấy mã giảm giá đã áp dụng từ session
        public Discount? GetAppliedDiscount()
        {
            var discountJson = _session.GetString(DiscountSessionKey);
            return discountJson == null
                ? null
                : JsonSerializer.Deserialize<Discount>(discountJson);
        }

        // Lưu mã giảm giá vào session
        private void SaveAppliedDiscount(Discount? discount)
        {
            if (discount == null)
            {
                _session.Remove(DiscountSessionKey);
            }
            else
            {
                _session.SetString(DiscountSessionKey, JsonSerializer.Serialize(discount));
            }
        }

        // Thêm sản phẩm vào giỏ hàng
        public void AddToCart(Product product, int quantity)
        {
            var cartItems = GetCartItems();
            var existingItem = cartItems.FirstOrDefault(item => item.ProductId == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cartItems.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    DiscountedPrice = product.DiscountedPrice,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity
                });
            }
            SaveCartItems(cartItems);
            // Xóa mã giảm giá nếu giỏ hàng thay đổi (để tránh áp dụng sai khi tổng tiền thay đổi)
            RemoveDiscount();
        }

        // Cập nhật số lượng sản phẩm trong giỏ
        public void UpdateCartItemQuantity(int productId, int quantity)
        {
            var cartItems = GetCartItems();
            var itemToUpdate = cartItems.FirstOrDefault(item => item.ProductId == productId);

            if (itemToUpdate != null)
            {
                if (quantity > 0)
                {
                    itemToUpdate.Quantity = quantity;
                }
                else
                {
                    cartItems.Remove(itemToUpdate);
                }
            }
            SaveCartItems(cartItems);
            RemoveDiscount(); // Xóa mã giảm giá
        }

        // Xóa sản phẩm khỏi giỏ hàng
        public void RemoveFromCart(int productId)
        {
            var cartItems = GetCartItems();
            var itemToRemove = cartItems.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cartItems.Remove(itemToRemove);
                SaveCartItems(cartItems);
            }
            RemoveDiscount(); // Xóa mã giảm giá
        }

        // Xóa toàn bộ giỏ hàng
        public void ClearCart()
        {
            _session.Remove(CartSessionKey);
            RemoveDiscount(); // Xóa mã giảm giá
        }

        // Lấy tổng số lượng sản phẩm trong giỏ
        public int GetCartItemCount()
        {
            return GetCartItems().Sum(item => item.Quantity);
        }

        // Lấy tổng giá trị ban đầu của giỏ hàng (chưa áp dụng giảm giá)
        public decimal GetCartSubtotal()
        {
            return GetCartItems().Sum(item => item.TotalPrice);
        }

        // Lấy tổng giá trị của giỏ hàng sau khi áp dụng giảm giá (nếu có)
        public decimal GetCartTotalPrice()
        {
            decimal subtotal = GetCartSubtotal();
            Discount? appliedDiscount = GetAppliedDiscount();

            if (appliedDiscount == null || !appliedDiscount.IsActive || appliedDiscount.EndDate < DateTime.Now)
            {
                return subtotal; // Không có mã giảm giá hợp lệ
            }

            // Kiểm tra giới hạn sử dụng
            if (appliedDiscount.UsageLimit.HasValue && appliedDiscount.UsedCount >= appliedDiscount.UsageLimit.Value)
            {
                return subtotal; // Mã giảm giá đã hết lượt sử dụng
            }

            // Kiểm tra giá trị đơn hàng tối thiểu
            if (appliedDiscount.MinimumOrderAmount.HasValue && subtotal < appliedDiscount.MinimumOrderAmount.Value)
            {
                return subtotal; // Đơn hàng chưa đạt giá trị tối thiểu
            }

            decimal discountedTotal = subtotal;

            if (appliedDiscount.DiscountType == "Percentage")
            {
                discountedTotal -= (subtotal * appliedDiscount.DiscountValue / 100);
            }
            else if (appliedDiscount.DiscountType == "FixedAmount")
            {
                discountedTotal -= appliedDiscount.DiscountValue;
            }

            // Đảm bảo tổng tiền không âm
            return Math.Max(0, discountedTotal);
        }

        // Áp dụng mã giảm giá
        public async Task<(bool Success, string Message)> ApplyDiscount(string discountCode)
        {
            var discount = await _context.Discounts.FirstOrDefaultAsync(d => d.Code == discountCode);

            if (discount == null)
            {
                return (false, "Mã giảm giá không tồn tại.");
            }

            if (!discount.IsActive)
            {
                return (false, "Mã giảm giá này không còn hoạt động.");
            }

            if (discount.StartDate > DateTime.Now)
            {
                return (false, "Mã giảm giá chưa đến ngày bắt đầu.");
            }

            if (discount.EndDate < DateTime.Now)
            {
                return (false, "Mã giảm giá đã hết hạn.");
            }

            if (discount.UsageLimit.HasValue && discount.UsedCount >= discount.UsageLimit.Value)
            {
                return (false, "Mã giảm giá đã hết lượt sử dụng.");
            }

            decimal subtotal = GetCartSubtotal();
            if (discount.MinimumOrderAmount.HasValue && subtotal < discount.MinimumOrderAmount.Value)
            {
                return (false, $"Đơn hàng tối thiểu để áp dụng mã này là {discount.MinimumOrderAmount.Value.ToString("C0", new System.Globalization.CultureInfo("vi-VN"))}.");
            }

            // Nếu mọi thứ hợp lệ, lưu mã giảm giá vào session
            SaveAppliedDiscount(discount);
            return (true, "Mã giảm giá đã được áp dụng thành công!");
        }

        // Xóa mã giảm giá đã áp dụng
        public void RemoveDiscount()
        {
            SaveAppliedDiscount(null);
        }
    }
}
