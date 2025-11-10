using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.IO;

// ===== QUAN TRỌNG: Đảm bảo Namespace và Area đúng =====
namespace DoAnWebBanDoHo.Areas.Admin.Controllers // Hoặc namespace đúng của bạn nếu không dùng Area
{
   // Xóa dòng này nếu controller không nằm trong Area "Admin"
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Products - (GIỮ NGUYÊN CODE PHÂN TRANG CỦA BẠN)
        public async Task<IActionResult> Index(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<Product> productsQuery = _context.Products.Include(p => p.Category);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    (p.Description != null && p.Description.Contains(searchTerm)) ||
                    (p.Category != null && p.Category.Name.Contains(searchTerm)));
            }

            int totalProducts = await productsQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            var products = await productsQuery
                                .OrderBy(p => p.Name)
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalProducts = totalProducts;

            return View(products);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Lấy sản phẩm VÀ lấy kèm Category VÀ ảnh Gallery
            var product = await _context.Products
                .Include(p => p.Category)       // Lấy kèm Category
                .Include(p => p.ProductImages)  // <-- THÊM DÒNG NÀY để lấy ảnh gallery
                .FirstOrDefaultAsync(m => m.Id == id); // Tìm theo ID

            if (product == null)
            {
                return NotFound(); // Trả về 404 nếu không tìm thấy
            }

            // Truyền product (đã có Category và ProductImages) ra View
            return View(product);
        }
        // GET: Products/Create (GIỮ NGUYÊN)
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }


        // POST: Products/Create (CODE ĐÃ SỬA LỖI VỊ TRÍ)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Name,Description,Price,DiscountedPrice,StockQuantity,ImageFile,CategoryId,SKU,Gender,Material,Origin,GalleryFiles")] Product product)
        {
            ModelState.Remove("ImageUrl"); // Bỏ qua vì gán thủ công

            if (ModelState.IsValid)
            {
                // ----- XỬ LÝ ẢNH CHÍNH -----
                if (product.ImageFile != null)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string productPath = Path.Combine(wwwRootPath, "images/products");
                    if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(product.ImageFile.FileName);
                    string path = Path.Combine(productPath, fileName);

                    using (var fileStream = new FileStream(path, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "/images/products/" + fileName;
                }
                else
                {
                    product.ImageUrl = "/images/placeholder.jpg";
                }

                // Gán ngày tạo/cập nhật
                product.CreatedDate = DateTime.Now;
                product.LastUpdated = DateTime.Now;

                // ----- LƯU SẢN PHẨM (Lần 1) -----
                _context.Add(product);
                await _context.SaveChangesAsync(); // <-- Lưu để lấy product.Id

                // ----- XỬ LÝ ẢNH GALLERY -----
                if (product.GalleryFiles != null && product.GalleryFiles.Count > 0)
                {
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string galleryPath = Path.Combine(wwwRootPath, "images/gallery");
                    if (!Directory.Exists(galleryPath)) Directory.CreateDirectory(galleryPath);

                    foreach (var file in product.GalleryFiles)
                    {
                        if (file.Length > 0)
                        {
                            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string filePath = Path.Combine(galleryPath, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var productImage = new ProductImage
                            {
                                ImageUrl = "/images/gallery/" + fileName,
                                ProductId = product.Id // <-- Gán ID sản phẩm
                            };
                            _context.ProductImages.Add(productImage);
                        }
                    }
                    // ----- LƯU ẢNH GALLERY (Lần 2) -----
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = $"Sản phẩm '{product.Name}' đã được tạo thành công.";
                return RedirectToAction(nameof(Index));

            } // <<<< Kết thúc if (ModelState.IsValid)

            // Nếu ModelState không hợp lệ, load lại Category và trả về View
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);

        } // <<<< Kết thúc action Create


        // GET: Products/Edit/5 (GIỮ NGUYÊN)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.ProductImages)

                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Thêm "GalleryFiles" vào Bind
        public async Task<IActionResult> Edit(int id,
            [Bind("Id,Name,Description,Price,DiscountedPrice,StockQuantity,ImageUrl,ImageFile,CategoryId,CreatedDate,SKU,Gender,Material,Origin,GalleryFiles")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            // Bỏ qua ImageUrl vì xử lý riêng
            ModelState.Remove("ImageUrl");
            // Bỏ qua GalleryFiles vì không phải là trường bắt buộc nhập
            ModelState.Remove("GalleryFiles");

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy bản ghi gốc không theo dõi để so sánh ảnh cũ
                    var existingProduct = await _context.Products
                                                .AsNoTracking()
                                                .FirstOrDefaultAsync(p => p.Id == id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // ----- Xử lý ẢNH CHÍNH -----
                    if (product.ImageFile != null)
                    {
                        // Xóa ảnh cũ
                        if (!string.IsNullOrEmpty(existingProduct.ImageUrl) && existingProduct.ImageUrl != "/images/placeholder.jpg")
                        {
                            string oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingProduct.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }
                        // Lưu ảnh mới
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(product.ImageFile.FileName);
                        string productPath = Path.Combine(wwwRootPath, "images/products");
                        if (!Directory.Exists(productPath)) Directory.CreateDirectory(productPath);
                        string path = Path.Combine(productPath, fileName);
                        using (var fileStream = new FileStream(path, FileMode.Create))
                        {
                            await product.ImageFile.CopyToAsync(fileStream);
                        }
                        product.ImageUrl = "/images/products/" + fileName;
                    }
                    else
                    {
                        // Giữ lại ảnh cũ nếu không upload ảnh mới
                        product.ImageUrl = existingProduct.ImageUrl;
                    }

                    // ----- Cập nhật thông tin sản phẩm chính -----
                    product.CreatedDate = existingProduct.CreatedDate; // Giữ ngày tạo
                    product.LastUpdated = DateTime.Now; // Cập nhật ngày sửa
                    _context.Update(product);
                    await _context.SaveChangesAsync(); // Lưu thay đổi sản phẩm chính

                    // ----- Xử lý THÊM MỚI ảnh Gallery -----
                    if (product.GalleryFiles != null && product.GalleryFiles.Count > 0)
                    {
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string galleryPath = Path.Combine(wwwRootPath, "images/gallery");
                        if (!Directory.Exists(galleryPath)) Directory.CreateDirectory(galleryPath);

                        foreach (var file in product.GalleryFiles)
                        {
                            if (file.Length > 0)
                            {
                                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                string filePath = Path.Combine(galleryPath, fileName);
                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }
                                var productImage = new ProductImage
                                {
                                    ImageUrl = "/images/gallery/" + fileName,
                                    ProductId = product.Id // Gán ID của sản phẩm đang sửa
                                };
                                _context.ProductImages.Add(productImage);
                            }
                        }
                        // Lưu các ảnh gallery MỚI vào DB
                        await _context.SaveChangesAsync();
                    }
                    // ----- Kết thúc xử lý Gallery -----

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                TempData["SuccessMessage"] = $"Sản phẩm '{product.Name}' đã được cập nhật thành công.";
                return RedirectToAction(nameof(Index));
            }
            // Nếu ModelState không hợp lệ
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            // Cần load lại ProductImages để hiển thị gallery cũ trên View khi có lỗi validation
            product.ProductImages = await _context.ProductImages.Where(pi => pi.ProductId == product.Id).ToListAsync();
            return View(product);
        }
        // GET: Products/Delete/5 (GIỮ NGUYÊN)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5 (Đã cập nhật để xóa cả ảnh gallery)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages) // Load ảnh gallery
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product != null)
            {
                // Xóa Ảnh Chính
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != "/images/placeholder.jpg")
                {
                    string imagePath = Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                // Xóa Ảnh Gallery
                foreach (var galleryImage in product.ProductImages)
                {
                    if (!string.IsNullOrEmpty(galleryImage.ImageUrl))
                    {
                        string galleryImagePath = Path.Combine(_hostEnvironment.WebRootPath, galleryImage.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(galleryImagePath))
                        {
                            System.IO.File.Delete(galleryImagePath);
                        }
                    }
                }
                _context.ProductImages.RemoveRange(product.ProductImages); // Xóa bản ghi trong DB

                // Xóa Sản phẩm
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Sản phẩm '{product.Name}' đã được xóa thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm để xóa.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}