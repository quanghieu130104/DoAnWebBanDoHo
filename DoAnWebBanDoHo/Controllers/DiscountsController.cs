using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Models;
using Microsoft.AspNetCore.Authorization; // <<<< Cần using này
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace DoAnWebBanDoHo.Controllers // Hoặc namespace có Area
{
    // [Area("Admin")] // Giữ lại nếu bạn dùng Area

    // BỎ Authorize ở đây đi
    // [Authorize(Roles = "Admin")]
    public class DiscountsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public DiscountsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Discounts (Danh sách - Chỉ Admin)
        [Authorize(Roles = "Admin")] // <<<< THÊM Authorize vào đây
        public async Task<IActionResult> Index()
        {
            return View(await _context.Discounts.OrderBy(d => d.EndDate).ToListAsync());
        }

        // GET: Discounts/Details/5 (Chi tiết - MỌI NGƯỜI)
        [AllowAnonymous] // <<<< THÊM AllowAnonymous vào đây
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var discount = await _context.Discounts.FirstOrDefaultAsync(m => m.Id == id);
            if (discount == null) return NotFound();
            return View(discount); // Trả về Views/Discounts/Details.cshtml
        }

        // GET: Discounts/Create (Form tạo mới - Chỉ Admin)
        [Authorize(Roles = "Admin")] // <<<< THÊM Authorize vào đây
        public IActionResult Create()
        {
            return View();
        }

        // POST: Discounts/Create (Xử lý tạo mới - Chỉ Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <<<< THÊM Authorize vào đây
        public async Task<IActionResult> Create([Bind("Id,Code,DiscountType,DiscountValue,MinimumOrderAmount,StartDate,EndDate,IsActive,UsageLimit,UsedCount,AppliesTo")] Discount discount)
        {
            // Bỏ qua validate ImageUrl nếu bạn copy nhầm từ code Banner
            ModelState.Remove("ImageUrl"); // Hoặc tên thuộc tính ảnh nếu có

            if (ModelState.IsValid)
            {
                // Logic lưu Discount (không có upload ảnh ở đây)
                discount.UsedCount = 0; // Đảm bảo số lần dùng ban đầu là 0
                _context.Add(discount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo mã giảm giá thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(discount);
        }

        // GET: Discounts/Edit/5 (Form sửa - Chỉ Admin)
        [Authorize(Roles = "Admin")] // <<<< THÊM Authorize vào đây
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null) return NotFound();
            return View(discount);
        }

        // POST: Discounts/Edit/5 (Xử lý sửa - Chỉ Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <<<< THÊM Authorize vào đây
        public async Task<IActionResult> Edit(int id, [Bind("Id,Code,DiscountType,DiscountValue,MinimumOrderAmount,StartDate,EndDate,IsActive,UsageLimit,UsedCount,AppliesTo")] Discount discount)
        {
            if (id != discount.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(discount);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật mã giảm giá thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiscountExists(discount.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(discount);
        }

        // GET: Discounts/Delete/5 (Xác nhận xóa - Chỉ Admin)
        [Authorize(Roles = "Admin")] // <<<< THÊM Authorize vào đây
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var discount = await _context.Discounts.FirstOrDefaultAsync(m => m.Id == id);
            if (discount == null) return NotFound();
            return View(discount);
        }

        // POST: Discounts/Delete/5 (Xử lý xóa - Chỉ Admin)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // <<<< THÊM Authorize vào đây
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount != null)
            {
                _context.Discounts.Remove(discount);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa mã giảm giá thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy mã giảm giá để xóa.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool DiscountExists(int id)
        {
            return _context.Discounts.Any(e => e.Id == id);
        }
    }
}