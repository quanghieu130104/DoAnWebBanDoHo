using System; // Required for Math.Ceiling
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data; // Đảm bảo namespace này đúng với ApplicationUser của bạn
using DoAnWebBanDoHo.Models; // Đảm bảo namespace này đúng với UserViewModel
using DoAnWebBanDoHo.Areas.Identity.Data;
namespace DoAnWebBanDoHo.Controllers
{
    // Chỉ người dùng có vai trò "Admin" mới được phép truy cập Controller này
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager; // Để quản lý người dùng
        private readonly RoleManager<IdentityRole> _roleManager;   // Để quản lý vai trò

        public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Users/Index - Hiển thị danh sách tất cả người dùng với tìm kiếm và phân trang
        public async Task<IActionResult> Index(string searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            IQueryable<ApplicationUser> usersQuery = _userManager.Users;

            // Apply search filter if searchTerm is provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Email.Contains(searchTerm) ||
                    u.FullName.Contains(searchTerm) ||
                    (u.Address != null && u.Address.Contains(searchTerm)));
            }

            // Calculate total users after filtering for pagination
            int totalUsers = await usersQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

            // Apply pagination
            var pagedUsers = await usersQuery
                                .OrderBy(u => u.Email) // Order by email for consistent pagination
                                .Skip((pageNumber - 1) * pageSize)
                                .Take(pageSize)
                                .ToListAsync();

            // Create UserViewModels for the paged users
            var userViewModels = new List<UserViewModel>();
            foreach (var user in pagedUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Address = user.Address,
                    DateOfBirth = user.DateOfBirth,
                    Roles = roles.ToList()
                });
            }

            // Pass pagination and search data via ViewBag
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.TotalUsers = totalUsers; // Total users for display info

            return View(userViewModels);
        }

        // GET: /Users/Edit/5 - Hiển thị form chỉnh sửa thông tin và vai trò của người dùng
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = await _roleManager.Roles.ToListAsync();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                UserRoles = userRoles.ToList(),
                AllRoles = allRoles.Select(r => r.Name).ToList()
            };

            return View(model);
        }

        // POST: /Users/Edit/5 - Xử lý cập nhật thông tin và vai trò của người dùng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Update basic user information (if fields are editable on the View)
            user.FullName = model.FullName;
            user.Address = model.Address;
            user.DateOfBirth = model.DateOfBirth;
            // Note: Email/UserName usually not directly editable via this page due to Identity complexities
            // If you update email, remember to update UserName as well.
            // user.Email = model.Email;
            // user.UserName = model.Email;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                // Reload roles for the view if there's an error
                model.UserRoles = (await _userManager.GetRolesAsync(user)).ToList();
                model.AllRoles = (await _roleManager.Roles.Select(r => r.Name).ToListAsync()).ToList();
                return View(model);
            }

            // Handle role updates
            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.SelectedRoles ?? new List<string>();

            // Remove roles no longer selected
            var rolesToRemove = userRoles.Except(selectedRoles);
            await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            // Add newly selected roles
            var rolesToAdd = selectedRoles.Except(userRoles);
            await _userManager.AddToRolesAsync(user, rolesToAdd);

            TempData["SuccessMessage"] = $"Người dùng '{user.Email}' đã được cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Delete/5 - Hiển thị trang xác nhận xóa người dùng
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Not allowed to delete the currently logged in user
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Address = user.Address,
                DateOfBirth = user.DateOfBirth,
                Roles = (await _userManager.GetRolesAsync(user)).ToList()
            };

            return View(userViewModel);
        }

        // POST: /Users/Delete/5 - Xử lý xóa người dùng
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Not allowed to delete the currently logged in user
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["ErrorMessage"] = "Bạn không thể xóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"Người dùng '{user.Email}' đã được xóa thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Lỗi khi xóa người dùng: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
