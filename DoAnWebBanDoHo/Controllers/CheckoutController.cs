using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Models;
using DoAnWebBanDoHo.Services;
using System.Security.Claims;
using DoAnWebBanDoHo.Areas.Identity.Data;

namespace DoAnWebBanDoHo.Controllers
{
    // Yêu cầu người dùng đăng nhập để tiến hành thanh toán
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(
            ApplicationDbContext context,
            CartService cartService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _cartService = cartService;
            _userManager = userManager;
        }

        // GET: /Checkout/Index - Hiển thị form thanh toán
        public async Task<IActionResult> Index()
        {
            var cartItems = _cartService.GetCartItems();
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm để thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy thông tin người dùng hiện tại để điền trước vào form
            var currentUser = await _userManager.GetUserAsync(User);
            var order = new Order
            {
                // Điền thông tin từ ApplicationUser nếu người dùng đã đăng nhập
                UserId = currentUser.Id,
                ReceiverName = currentUser.FullName,
                Email = currentUser.Email,
                PhoneNumber = currentUser.PhoneNumber,
                ShippingAddress = currentUser.Address // Lấy địa chỉ từ thông tin người dùng
            };

            // Truyền giỏ hàng và thông tin người dùng để hiển thị trên trang checkout
            ViewBag.CartItems = cartItems;
            ViewBag.CartSubtotal = _cartService.GetCartSubtotal(); // <<<< Truyền tổng tiền chưa giảm giá
            ViewBag.CartTotalPrice = _cartService.GetCartTotalPrice();
            ViewBag.AppliedDiscount = _cartService.GetAppliedDiscount(); // <<<< Truyền mã giảm giá đã áp dụng

            return View(order);
        }

        // POST: /Checkout/PlaceOrder - Xử lý đặt hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder([Bind("ReceiverName,PhoneNumber,ShippingAddress,Email,Notes,PaymentMethod")] Order order)
        {
            var cartItems = _cartService.GetCartItems();
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống. Không thể tạo đơn hàng.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy người dùng hiện tại
            var currentUser = await _userManager.GetUserAsync(User);
            order.UserId = currentUser.Id; // Gán UserId cho đơn hàng

            // Lấy mã giảm giá đã áp dụng (nếu có)
            var appliedDiscount = _cartService.GetAppliedDiscount();

            // Tính toán tổng tiền từ giỏ hàng (đảm bảo không bị sửa đổi từ client)
            order.TotalAmount = _cartService.GetCartTotalPrice(); // Total amount đã bao gồm giảm giá
            order.OrderDate = DateTime.Now;
            order.OrderStatus = "Chờ xác nhận"; // Trạng thái mặc định

            // Kiểm tra tính hợp lệ của model và số lượng tồn kho
            if (ModelState.IsValid)
            {
                // Kiểm tra lại số lượng tồn kho trước khi đặt hàng để tránh trường hợp mua quá số lượng
                foreach (var cartItem in cartItems)
                {
                    var productInDb = await _context.Products.FindAsync(cartItem.ProductId);
                    if (productInDb == null || productInDb.StockQuantity < cartItem.Quantity)
                    {
                        TempData["ErrorMessage"] = $"Sản phẩm '{cartItem.ProductName}' không đủ số lượng trong kho. Vui lòng kiểm tra lại giỏ hàng.";
                        // Cập nhật lại ViewBag để redisplay form với thông tin giỏ hàng
                        ViewBag.CartItems = cartItems;
                        ViewBag.CartSubtotal = _cartService.GetCartSubtotal();
                        ViewBag.CartTotalPrice = _cartService.GetCartTotalPrice();
                        ViewBag.AppliedDiscount = appliedDiscount;
                        return View("Index", order); // Hiển thị lại form với lỗi
                    }
                }

                _context.Add(order);
                await _context.SaveChangesAsync(); // Lưu Order trước để có Order.Id

                // Nếu có mã giảm giá đã áp dụng thành công, tăng UsedCount của mã giảm giá
                if (appliedDiscount != null)
                {
                    var discountInDb = await _context.Discounts.FindAsync(appliedDiscount.Id);
                    if (discountInDb != null)
                    {
                        discountInDb.UsedCount++;
                        _context.Update(discountInDb);
                    }
                }

                // Tạo OrderItems và cập nhật số lượng tồn kho
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        ProductName = cartItem.ProductName,
                        Price = cartItem.Price,
                        DiscountedPrice = cartItem.DiscountedPrice,
                        Quantity = cartItem.Quantity
                    };
                    _context.Add(orderItem);

                    // Cập nhật số lượng tồn kho của sản phẩm
                    var productToUpdate = await _context.Products.FindAsync(cartItem.ProductId);
                    if (productToUpdate != null)
                    {
                        productToUpdate.StockQuantity -= cartItem.Quantity;
                        _context.Update(productToUpdate);
                    }
                }
                await _context.SaveChangesAsync(); // Lưu OrderItems và cập nhật Products (và Discount)

                _cartService.ClearCart(); // Xóa giỏ hàng sau khi đặt hàng thành công
                TempData["SuccessMessage"] = $"Đơn hàng của bạn (#ORD{order.Id}) đã được đặt thành công!";

                return RedirectToAction("OrderConfirmation", new { orderId = order.Id });
            }

            // Nếu model không hợp lệ, hiển thị lại form với lỗi
            ViewBag.CartItems = cartItems;
            ViewBag.CartSubtotal = _cartService.GetCartSubtotal();
            ViewBag.CartTotalPrice = _cartService.GetCartTotalPrice();
            ViewBag.AppliedDiscount = appliedDiscount;
            return View("Index", order);
        }

        // GET: /Checkout/OrderConfirmation - Trang xác nhận đơn hàng
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _context.Orders
                                    .Include(o => o.OrderItems)
                                    .ThenInclude(oi => oi.Product)
                                    .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == _userManager.GetUserId(User));

            if (order == null)
            {
                return NotFound();
            }

            // Xóa mã giảm giá khỏi session sau khi đơn hàng đã được xác nhận thành công
            _cartService.RemoveDiscount();

            return View(order);
        }
    }
}
