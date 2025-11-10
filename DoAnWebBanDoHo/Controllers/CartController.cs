using Microsoft.AspNetCore.Mvc;
using DoAnWebBanDoHo.Services;
using DoAnWebBanDoHo.Data;
using Microsoft.EntityFrameworkCore;

namespace DoAnWebBanDoHo.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;
        private readonly ApplicationDbContext _context;

        public CartController(CartService cartService, ApplicationDbContext context)
        {
            _cartService = cartService;
            _context = context;
        }

        // GET: /Cart/Index - Display shopping cart content
        public IActionResult Index()
        {
            var cartItems = _cartService.GetCartItems();
            ViewBag.CartSubtotal = _cartService.GetCartSubtotal(); // Tổng tiền chưa giảm giá
            ViewBag.CartTotalPrice = _cartService.GetCartTotalPrice(); // Tổng tiền sau giảm giá
            ViewBag.AppliedDiscount = _cartService.GetAppliedDiscount(); // Mã giảm giá đã áp dụng

            return View(cartItems);
        }

        // POST: /Cart/AddToCart - Add product to cart (from product details page, etc.)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Số lượng phải lớn hơn 0.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            // Kiểm tra số lượng tồn kho trước khi thêm vào giỏ hàng
            var currentCartItem = _cartService.GetCartItems().FirstOrDefault(item => item.ProductId == productId);
            var quantityInCart = currentCartItem?.Quantity ?? 0;
            if (product.StockQuantity < (quantityInCart + quantity))
            {
                TempData["ErrorMessage"] = $"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} trong kho. Bạn đã có {quantityInCart} sản phẩm này trong giỏ.";
                return RedirectToAction("Details", "Home", new { id = productId });
            }

            _cartService.AddToCart(product, quantity);
            TempData["SuccessMessage"] = $"Đã thêm {quantity} sản phẩm '{product.Name}' vào giỏ hàng.";
            return RedirectToAction(nameof(Index)); // Redirect to cart index
        }

        // POST: /Cart/UpdateQuantity - Update quantity of a product in cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                return RedirectToAction(nameof(Index));
            }

            if (quantity <= 0)
            {
                _cartService.RemoveFromCart(productId); // Xóa nếu số lượng là 0 hoặc âm
                TempData["SuccessMessage"] = "Sản phẩm đã được xóa khỏi giỏ hàng.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra số lượng tồn kho khi cập nhật
            if (product.StockQuantity < quantity)
            {
                TempData["ErrorMessage"] = $"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} trong kho. Vui lòng giảm số lượng.";
                return RedirectToAction(nameof(Index));
            }

            _cartService.UpdateCartItemQuantity(productId, quantity);
            TempData["SuccessMessage"] = "Số lượng sản phẩm đã được cập nhật.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/RemoveItem - Remove a product from cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveItem(int productId)
        {
            _cartService.RemoveFromCart(productId);
            TempData["SuccessMessage"] = "Sản phẩm đã được xóa khỏi giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/ClearCart - Clear the entire cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCart()
        {
            _cartService.ClearCart();
            TempData["SuccessMessage"] = "Giỏ hàng của bạn đã được làm trống.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/ApplyDiscount - Áp dụng mã giảm giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyDiscount(string discountCode)
        {
            if (string.IsNullOrWhiteSpace(discountCode))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mã giảm giá.";
                return RedirectToAction(nameof(Index));
            }

            var (success, message) = await _cartService.ApplyDiscount(discountCode);
            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpGet] // Dùng GET vì link <a> mặc định là GET
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1) // Nhận productId và quantity (mặc định là 1)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại.";
                // Chuyển về trang chủ hoặc trang sản phẩm nếu không tìm thấy
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra tồn kho trước khi thêm
            var currentCartItem = _cartService.GetCartItems().FirstOrDefault(item => item.ProductId == productId);
            var quantityInCart = currentCartItem?.Quantity ?? 0;
            if (product.StockQuantity < quantity) // Kiểm tra xem có đủ số lượng MUỐN mua ngay không
            {
                TempData["ErrorMessage"] = $"Sản phẩm '{product.Name}' chỉ còn {product.StockQuantity} trong kho.";
                return RedirectToAction("Details", "Home", new { id = productId }); // Quay lại trang chi tiết
            }
            // Optional: Kiểm tra tổng số lượng (mua ngay + đã có trong giỏ) nếu logic yêu cầu
            // if (product.StockQuantity < (quantityInCart + quantity)) { ... } 

            // Thêm sản phẩm vào giỏ (hoặc cập nhật số lượng nếu đã có)
            _cartService.AddToCart(product, quantity); // Sử dụng phương thức AddToCart đã có (nếu nó nhận Product)
                                                       // hoặc _cartService.AddToCart(productId, quantity);

            // Chuyển hướng đến trang Checkout
            // !!! THAY CheckoutController và Index bằng tên Controller/Action trang thanh toán CỦA BẠN !!!
            return RedirectToAction("Index", "Checkout");
        }

        // POST: /Cart/RemoveDiscount - Xóa mã giảm giá đã áp dụng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveDiscount()
        {
            _cartService.RemoveDiscount();
            TempData["SuccessMessage"] = "Mã giảm giá đã được gỡ bỏ.";
            return RedirectToAction(nameof(Index));
        }
    }
}
