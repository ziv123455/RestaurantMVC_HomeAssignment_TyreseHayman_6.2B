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

namespace RestaurantMVC.Controllers
{
    [Authorize]
    public class VerificationController : Controller
    {
        private readonly RestaurantDbContext _context;
        private const string SiteAdminEmail = "siteadmin@example.com";

        public VerificationController(RestaurantDbContext context)
        {
            _context = context;
        }

        private string? CurrentEmail =>
            User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

        private bool IsAdmin =>
            !string.IsNullOrEmpty(CurrentEmail) &&
            string.Equals(CurrentEmail, SiteAdminEmail, StringComparison.OrdinalIgnoreCase);

        // --------------------------------------------------------------------
        // ADMIN: verify RESTAURANTS
        // --------------------------------------------------------------------

        // GET: /Verification/Admin
        [HttpGet]
        public async Task<IActionResult> Admin()
        {
            if (!IsAdmin)
                return Forbid();

            var restaurants = await _context.Restaurants
                .Where(r => r.Status == "Pending")
                .ToListAsync();

            return View(restaurants); // uses Views/Verification/Admin.cshtml
        }

        // POST: /Verification/ApproveRestaurants
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRestaurants(List<int> selectedRestaurantIds)
        {
            if (!IsAdmin)
                return Forbid();

            if (selectedRestaurantIds == null || selectedRestaurantIds.Count == 0)
                return RedirectToAction(nameof(Admin));

            var toApprove = await _context.Restaurants
                .Where(r => selectedRestaurantIds.Contains(r.Id))
                .ToListAsync();

            foreach (var r in toApprove)
            {
                r.Status = "Approved";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Admin));
        }

        // --------------------------------------------------------------------
        // RESTAURANT OWNERS: verify OWN MENU ITEMS
        // --------------------------------------------------------------------

        // GET: /Verification (Index)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (IsAdmin || string.IsNullOrEmpty(CurrentEmail))
                return Forbid();

            var items = await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => m.Status == "Pending"
                            && m.Restaurant != null
                            && m.Restaurant.Status == "Approved"
                            && m.Restaurant.OwnerEmailAddress == CurrentEmail)
                .ToListAsync();

            return View(items); // uses Views/Verification/Index.cshtml
        }

        // POST: /Verification/ApproveMenuItems
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMenuItems(List<Guid> selectedMenuItemIds)
        {
            if (IsAdmin || string.IsNullOrEmpty(CurrentEmail))
                return Forbid();

            if (selectedMenuItemIds == null || selectedMenuItemIds.Count == 0)
                return RedirectToAction(nameof(Index));

            var items = await _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => selectedMenuItemIds.Contains(m.Id)
                            && m.Restaurant != null
                            && m.Restaurant.OwnerEmailAddress == CurrentEmail)
                .ToListAsync();

            foreach (var m in items)
            {
                m.Status = "Approved";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
