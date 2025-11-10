using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Models;
using Microsoft.AspNetCore.Authorization; // Cho Authorize
using Microsoft.AspNetCore.Hosting;      // Cho IWebHostEnvironment
using System.IO;                       // Cho Path

namespace DoAnWebBanDoHo.Controllers // Hoặc DoAnWebBanDoHo.Areas.Admin.Controllers
{
    // [Area("Admin")] // Thêm dòng này nếu Controller nằm trong Area Admin
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được truy cập
    public class BannersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // Inject để lấy wwwroot path

        public BannersController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Banners (Danh sách banner)
        public async Task<IActionResult> Index()
        {
            // Sắp xếp theo thứ tự hiển thị
            return View(await _context.Banners.OrderBy(b => b.DisplayOrder).ToListAsync());
        }

        // GET: Banners/Details/5 (Xem chi tiết - có thể không cần thiết)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FirstOrDefaultAsync(m => m.Id == id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        // GET: Banners/Create (Form tạo mới)
        public IActionResult Create()
        {
            return View();
        }

        // POST: Banners/Create (Xử lý tạo mới)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,LinkUrl,DisplayOrder,IsActive,ImageFile")] Banner banner)
        {
            // Bỏ qua validate ImageUrl vì sẽ gán sau khi upload
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                // ----- XỬ LÝ UPLOAD ẢNH -----
                if (banner.ImageFile != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string bannerPath = Path.Combine(wwwRootPath, "images/banners"); // Thư mục lưu banner
                    if (!Directory.Exists(bannerPath))
                    {
                        Directory.CreateDirectory(bannerPath);
                    }

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(banner.ImageFile.FileName);
                    string filePath = Path.Combine(bannerPath, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await banner.ImageFile.CopyToAsync(fileStream);
                    }
                    banner.ImageUrl = "/images/banners/" + fileName; // Lưu đường dẫn tương đối
                }
                else
                {
                    // Nếu không upload, có thể báo lỗi hoặc dùng ảnh mặc định
                    ModelState.AddModelError("ImageFile", "Vui lòng chọn ảnh banner.");
                    return View(banner); // Quay lại form
                    // banner.ImageUrl = "/images/placeholder-banner.jpg"; // Hoặc dùng ảnh mặc định
                }
                // ----- KẾT THÚC UPLOAD -----

                _context.Add(banner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã tạo banner thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(banner); // Nếu model không hợp lệ, quay lại form
        }

        // GET: Banners/Edit/5 (Form chỉnh sửa)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FindAsync(id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        // POST: Banners/Edit/5 (Xử lý chỉnh sửa)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,LinkUrl,DisplayOrder,IsActive,ImageUrl,ImageFile")] Banner banner)
        {
            if (id != banner.Id) return NotFound();

            // Bỏ qua validate ImageUrl vì có thể giữ ảnh cũ
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBanner = await _context.Banners.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
                    if (existingBanner == null) return NotFound();

                    // ----- XỬ LÝ UPLOAD ẢNH MỚI (NẾU CÓ) -----
                    if (banner.ImageFile != null)
                    {
                        // Xóa ảnh cũ (nếu có)
                        if (!string.IsNullOrEmpty(existingBanner.ImageUrl))
                        {
                            string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingBanner.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Lưu ảnh mới
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string bannerPath = Path.Combine(wwwRootPath, "images/banners");
                        if (!Directory.Exists(bannerPath)) Directory.CreateDirectory(bannerPath);

                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(banner.ImageFile.FileName);
                        string filePath = Path.Combine(bannerPath, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await banner.ImageFile.CopyToAsync(fileStream);
                        }
                        banner.ImageUrl = "/images/banners/" + fileName; // Cập nhật đường dẫn mới
                    }
                    else
                    {
                        // Nếu không upload ảnh mới, giữ lại ảnh cũ
                        banner.ImageUrl = existingBanner.ImageUrl;
                    }
                    // ----- KẾT THÚC UPLOAD -----

                    _context.Update(banner);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Đã cập nhật banner thành công.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BannerExists(banner.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(banner); // Nếu model không hợp lệ
        }

        // GET: Banners/Delete/5 (Trang xác nhận xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FirstOrDefaultAsync(m => m.Id == id);
            if (banner == null) return NotFound();
            return View(banner);
        }

        // POST: Banners/Delete/5 (Xử lý xóa)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                // Xóa file ảnh vật lý trước khi xóa bản ghi
                if (!string.IsNullOrEmpty(banner.ImageUrl))
                {
                    string imagePath = Path.Combine(_hostEnvironment.WebRootPath, banner.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa banner thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy banner để xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool BannerExists(int id)
        {
            return _context.Banners.Any(e => e.Id == id);
        }
    }
}