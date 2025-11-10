using DoAnWebBanDoHo.Areas.Identity.Data;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Required for SelectList
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DoAnWebBanDoHo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<HomeController> logger)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        // Updated Index method to support category filtering and search
        public async Task<IActionResult> Index(int? categoryId, string? searchTerm) // Added searchTerm
        {
            // Get all categories for the dropdown filter
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", categoryId);
            // Pass the current search term back to the view
            ViewData["CurrentSearchTerm"] = searchTerm;

            // Start the product query
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // Apply category filter if categoryId is provided and valid
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // Apply search term filter if searchTerm is provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Search in Product Name or Description (case-insensitive)
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm));
            }

            // Execute the query and pass the filtered/searched products to the view
            var products = await productsQuery.ToListAsync();
            var banners = await _context.Banners
                                    .Where(b => b.IsActive)        // Chỉ lấy banner đang hoạt động
                                    .OrderBy(b => b.DisplayOrder) // Sắp xếp theo thứ tự
                                    .ToListAsync();
            ViewBag.Banners = banners;
            // ===== LẤY SẢN PHẨM ĐÃ XEM =====
            string cookieName = "RecentlyViewedProducts";
            string viewedCookieValue = Request.Cookies[cookieName] ?? "";
            List<int> viewedProductIdsInt = viewedCookieValue
                                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(int.Parse) // Chuyển chuỗi ID thành số nguyên
                                                .ToList();

            List<Product> recentlyViewedProducts = new List<Product>();
            if (viewedProductIdsInt.Any())
            {
                // Lấy thông tin sản phẩm từ DB theo các ID đã lưu
                recentlyViewedProducts = await _context.Products
                                                    .Where(p => viewedProductIdsInt.Contains(p.Id))
                                                    .ToListAsync();
                // Sắp xếp lại theo thứ tự trong cookie (xem gần nhất lên đầu)
                recentlyViewedProducts = recentlyViewedProducts
                                            .OrderBy(p => viewedProductIdsInt.IndexOf(p.Id))
                                            .ToList();
            }
            ViewBag.RecentlyViewedProducts = recentlyViewedProducts; // Gửi ra View
            return View(products);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Chỉ người dùng đã đăng nhập mới được post review
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                // Trường hợp lỗi không lấy được user dù đã Authorize
                return Challenge(); // Chuyển đến trang đăng nhập
            }

            // Tạo đối tượng Review mới
            var review = new ProductReview
            {
                ProductId = productId,
                UserId = user.Id,
                ReviewerName = user.UserName ?? user.Email, // Lấy Tên hoặc Email làm tên người review
                Rating = rating,
                Comment = comment,
                ReviewDate = DateTime.Now,
                IsApproved = true // Tự động duyệt hoặc đặt là false nếu cần Admin duyệt
            };

            // Kiểm tra validation thủ công (vì không dùng model binding trực tiếp)
            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Vui lòng chọn số sao từ 1 đến 5.";
                return RedirectToAction("Details", new { id = productId });
            }
            // Thêm các kiểm tra khác nếu cần (vd: độ dài comment)


            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Cảm ơn bạn đã gửi đánh giá!";
            return RedirectToAction("Details", new { id = productId }); // Quay lại trang chi tiết
        }
        // ===========================
        // GET: Home/Details/5 - Display product details for the user
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages) // <<<< THÊM DÒNG NÀY VÀO ĐÂY
                .Include(p => p.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            // Tính toán thông tin Rating (Gửi qua ViewBag)
            if (product.Reviews != null && product.Reviews.Any(r => r.IsApproved))
            {
                // Chỉ tính trên các review đã được duyệt
                var approvedReviews = product.Reviews.Where(r => r.IsApproved).ToList();

                if (approvedReviews.Any()) // Kiểm tra lại sau khi lọc
                {
                    ViewBag.AverageRating = approvedReviews.Average(r => r.Rating);
                    ViewBag.ReviewCount = approvedReviews.Count;

                    // DÙNG DICTIONARY<int, int> (An toàn hơn List<dynamic>)
                    ViewBag.RatingCounts = approvedReviews
                                                .GroupBy(r => r.Rating)
                                                .ToDictionary(g => g.Key, g => g.Count());
                }
                else
                {
                    ViewBag.AverageRating = 0;
                    ViewBag.ReviewCount = 0;
                    ViewBag.RatingCounts = new Dictionary<int, int>(); // Dictionary rỗng
                }
            }
            else
            {
                ViewBag.AverageRating = 0;
                ViewBag.ReviewCount = 0;
                ViewBag.RatingCounts = new Dictionary<int, int>(); // Dictionary rỗng
            }
            var discounts = await _context.Discounts
                                .Where(d => d.IsActive && d.EndDate >= DateTime.Today) // Còn hạn và Active
                                .OrderBy(d => d.EndDate) // Sắp xếp
                                .ToListAsync();
            ViewBag.Discounts = discounts;
            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
