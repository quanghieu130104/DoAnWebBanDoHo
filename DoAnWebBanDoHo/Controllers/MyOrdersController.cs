using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data; // <<<<<< Dòng này bây giờ bao gồm ApplicationUser
using DoAnWebBanDoHo.Models;
using DoAnWebBanDoHo.Areas.Identity.Data;

namespace DoAnWebBanDoHo.Controllers
{
    [Authorize]
    public class MyOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MyOrdersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            var orders = await _context.Orders
                                    .Where(o => o.UserId == userId)
                                    .Include(o => o.OrderItems)
                                        .ThenInclude(oi => oi.Product)
                                    .OrderByDescending(o => o.OrderDate)
                                    .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                                    .Include(o => o.OrderItems)
                                        .ThenInclude(oi => oi.Product)
                                    .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}
