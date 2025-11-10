using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Models;
using System.Collections.Generic;

namespace DoAnWebBanDoHo.Controllers
{
    // Chỉ Admin mới có thể truy cập Controller này
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports/Index - Hiển thị trang báo cáo tổng quan
        public async Task<IActionResult> Index()
        {
            var viewModel = new SalesStatisticsViewModel();

            // 1. Tổng Doanh Thu, Tổng Số Đơn Hàng, Tổng Số Sản Phẩm Đã Bán
            // Các truy vấn này đơn giản và EF Core có thể dịch tốt
            viewModel.TotalOverallRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);
            viewModel.TotalOrders = await _context.Orders.CountAsync();
            viewModel.TotalItemsSold = await _context.OrderItems.SumAsync(oi => oi.Quantity);

            // 2. Sản Phẩm Bán Chạy Nhất (Top 5)
            // Truy vấn này phức tạp hơn nhưng thường được EF Core hỗ trợ tốt với Include và GroupBy
            viewModel.TopSellingProducts = await _context.OrderItems
                .Include(oi => oi.Product) // Bao gồm thông tin sản phẩm để lấy ImageUrl
                .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.Product.ImageUrl })
                .Select(g => new ProductSale
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ImageUrl = g.Key.ImageUrl,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => (oi.DiscountedPrice.HasValue ? oi.DiscountedPrice.Value : oi.Price) * oi.Quantity)
                })
                .OrderByDescending(ps => ps.TotalQuantitySold)
                .Take(5)
                .ToListAsync();

            // 3. Doanh Thu Theo Tháng (của năm hiện tại)
            // Lấy tất cả đơn hàng của năm hiện tại vào bộ nhớ trước
            var ordersCurrentYear = await _context.Orders
                                                .Where(o => o.OrderDate.Year == DateTime.Now.Year)
                                                .ToListAsync(); // Thực thi truy vấn SQL ở đây

            // Sau đó, thực hiện GroupBy và tính toán trong bộ nhớ (LINQ to Objects)
            viewModel.MonthlyRevenue = ordersCurrentYear
                .GroupBy(o => o.OrderDate.Month)
                .OrderBy(g => g.Key) // Sắp xếp theo số tháng
                .ToDictionary(g => $"{g.Key}/{DateTime.Now.Year}", g => g.Sum(o => o.TotalAmount));

            // 4. Doanh Thu Theo Năm (trong 5 năm gần đây)
            // Lấy tất cả đơn hàng trong 5 năm gần đây vào bộ nhớ trước
            var ordersRecentYears = await _context.Orders
                                                .Where(o => o.OrderDate.Year >= DateTime.Now.Year - 4)
                                                .ToListAsync(); // Thực thi truy vấn SQL ở đây

            // Sau đó, thực hiện GroupBy và tính toán trong bộ nhớ (LINQ to Objects)
            viewModel.YearlyRevenue = ordersRecentYears
                .GroupBy(o => o.OrderDate.Year)
                .OrderBy(g => g.Key) // Sắp xếp theo năm
                .ToDictionary(g => g.Key.ToString(), g => g.Sum(o => o.TotalAmount));

            return View(viewModel);
        }
    }
}
