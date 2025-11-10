using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Models;
using System.Linq;
using System.Threading.Tasks;
using System; // Cần cho Math.Ceiling

namespace DoAnWebBanDoHo.Controllers
{
    // Chỉ Admin mới có thể truy cập OrderController
    [Authorize(Roles = "Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders - Hiển thị danh sách tất cả các đơn hàng với tìm kiếm và phân trang
        public async Task<IActionResult> Index(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<Order> ordersQuery = _context.Orders
                                    .Include(o => o.User)
                                    .Include(o => o.OrderItems);

            // Áp dụng tìm kiếm nếu có searchTerm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.ReceiverName.Contains(searchTerm) ||
                    o.Email.Contains(searchTerm) ||
                    o.PhoneNumber.Contains(searchTerm) ||
                    o.ShippingAddress.Contains(searchTerm) ||
                    o.OrderStatus.Contains(searchTerm) ||
                    o.Id.ToString().Contains(searchTerm) // Tìm kiếm theo ID đơn hàng
                );
            }

            // Tính toán tổng số đơn hàng sau khi lọc để phân trang
            int totalOrders = await ordersQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            // Áp dụng phân trang
            var orders = await ordersQuery
                                .OrderByDescending(o => o.OrderDate) // Sắp xếp theo ngày đặt hàng mới nhất
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            // Truyền dữ liệu phân trang và tìm kiếm qua ViewBag
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalOrders = totalOrders; // Tổng số đơn hàng để hiển thị thông tin

            return View(orders);
        }

        // GET: Orders/Details/5 - Hiển thị chi tiết đơn hàng
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                                    .Include(o => o.User)
                                    .Include(o => o.OrderItems)
                                        .ThenInclude(oi => oi.Product) // Bao gồm thông tin Product cho từng OrderItem
                                    .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Edit/5 - Hiển thị form chỉnh sửa trạng thái đơn hàng
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5 - Xử lý cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,OrderStatus,Notes")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            // ✅ KHÔNG dùng AsNoTracking
            var existingOrder = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (existingOrder == null)
            {
                return NotFound();
            }

            // ✅ Cập nhật đúng
            existingOrder.OrderStatus = order.OrderStatus;
            existingOrder.Notes = order.Notes;

            if (ModelState.IsValid)
            {
                try
                {
                    await _context.SaveChangesAsync(); // Không cần gọi Update()
                    TempData["SuccessMessage"] = $"Đơn hàng #{order.Id} đã được cập nhật.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Orders.Any(e => e.Id == order.Id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(order);
        }




        // Helper method để kiểm tra Order tồn tại
        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
