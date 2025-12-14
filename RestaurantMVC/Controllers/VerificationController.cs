using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Models;

namespace RestaurantMVC.Controllers
{
    [Authorize] // must be logged in
    public class VerificationController : Controller
    {
        private readonly RestaurantDbContext _db;
        private const string SiteAdminEmail = "siteadmin@example.com";

        public VerificationController(RestaurantDbContext db)
        {
            _db = db;
        }

        private bool IsAdmin()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;
            return !string.IsNullOrEmpty(email) &&
                   string.Equals(email, SiteAdminEmail, StringComparison.OrdinalIgnoreCase);
        }

        // GET: /Verification/Admin
        [HttpGet]
        public async Task<IActionResult> Admin()
        {
            if (!IsAdmin())
                return Forbid();

            var vm = new VerificationViewModel
            {
                PendingRestaurants = await _db.Restaurants
                    .Where(r => r.Status == "Pending")
                    .ToListAsync(),

                PendingMenuItems = await _db.MenuItems
                    .Include(m => m.Restaurant)
                    .Where(m => m.Status == "Pending")
                    .ToListAsync()
            };

            return View(vm);
        }

        // POST: approve selected restaurants
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRestaurants(List<int> selectedRestaurantIds)
        {
            if (!IsAdmin())
                return Forbid();

            if (selectedRestaurantIds != null && selectedRestaurantIds.Count > 0)
            {
                var restaurants = await _db.Restaurants
                    .Where(r => selectedRestaurantIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var r in restaurants)
                    r.Status = "Approved";

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Admin));
        }

        // POST: approve selected menu items
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMenuItems(List<Guid> selectedMenuIds)
        {
            if (!IsAdmin())
                return Forbid();

            if (selectedMenuIds != null && selectedMenuIds.Count > 0)
            {
                var menuItems = await _db.MenuItems
                    .Where(m => selectedMenuIds.Contains(m.Id))
                    .ToListAsync();

                foreach (var m in menuItems)
                    m.Status = "Approved";

                await _db.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Admin));
        }
    }
}
