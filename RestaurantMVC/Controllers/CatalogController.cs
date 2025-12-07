using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess;
using DataAccess.Repositories;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RestaurantMVC.Controllers
{
    public class CatalogController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly ItemsDbRepository _dbRepository;

        // Same admin as VerificationController + _Layout
        private const string SiteAdminEmail = "siteadmin@example.com";

        public CatalogController(RestaurantDbContext context, ItemsDbRepository dbRepository)
        {
            _context = context;
            _dbRepository = dbRepository;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string type = "restaurants",
            string view = "card",
            string mode = "view",
            int? restaurantId = null)
        {
            ViewBag.Type = type;
            ViewBag.Mode = mode;
            ViewBag.ViewMode = view;

            // If someone tries mode=approve, restrict it to site admin only
            if (string.Equals(mode, "approve", StringComparison.OrdinalIgnoreCase))
            {
                var user = User;
                var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;

                if (string.IsNullOrEmpty(email) ||
                    !email.Equals(SiteAdminEmail, StringComparison.OrdinalIgnoreCase))
                {
                    // Normal users can't see pending items in catalog
                    return Forbid();
                }
            }

            var result = new List<IItemValidating>();

            if (string.Equals(type, "restaurants", StringComparison.OrdinalIgnoreCase))
            {
                var query = _context.Restaurants.AsQueryable();

                if (string.Equals(mode, "approve", StringComparison.OrdinalIgnoreCase))
                {
                    // Approve mode: pending only
                    query = query.Where(r => r.Status == "Pending");
                }
                else
                {
                    // View mode (and default): approved only
                    query = query.Where(r => r.Status == "Approved");
                }

                result = await query.ToListAsync<IItemValidating>();
            }
            else if (string.Equals(type, "menu", StringComparison.OrdinalIgnoreCase))
            {
                var query = _context.MenuItems
                    .Include(m => m.Restaurant)
                    .AsQueryable();

                if (restaurantId.HasValue)
                {
                    query = query.Where(m => m.RestaurantId == restaurantId.Value);
                }

                if (string.Equals(mode, "approve", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(m => m.Status == "Pending");
                }
                else
                {
                    query = query.Where(m => m.Status == "Approved");
                }

                result = await query.ToListAsync<IItemValidating>();
            }

            return View(result);
        }

        // POST: /Catalog/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Approve(
            string type,
            List<int>? restaurantIds,
            List<Guid>? menuItemIds,
            string view,
            string mode)
        {
            restaurantIds ??= new List<int>();
            menuItemIds ??= new List<Guid>();

            var user = User;
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;

            // Only site admin should be able to approve via catalog
            if (string.IsNullOrEmpty(email) ||
                !email.Equals(SiteAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            await _dbRepository.ApproveAsync(
                type == "restaurants" ? restaurantIds : new List<int>(),
                type == "menu" ? menuItemIds : new List<Guid>());

            // Redirect back to approve mode for same type & view
            return RedirectToAction("Index", new { type = type, view = view, mode = "approve" });
        }
    }
}
