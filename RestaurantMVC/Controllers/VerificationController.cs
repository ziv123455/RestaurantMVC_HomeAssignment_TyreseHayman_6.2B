using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess;
using DataAccess.Repositories;
using Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantMVC.Filters;

namespace RestaurantMVC.Controllers
{
    [Authorize]
    public class VerificationController : Controller
    {
        private readonly RestaurantDbContext _context;
        private readonly ItemsDbRepository _dbRepository;

        // Hard-coded site admin email (must match a registered user)
        private const string SiteAdminEmail = "siteadmin@example.com";

        public VerificationController(RestaurantDbContext context, ItemsDbRepository dbRepository)
        {
            _context = context;
            _dbRepository = dbRepository;
        }

        public async Task<IActionResult> Index(int? restaurantId)
        {
            var user = User;
            var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name;

            if (string.IsNullOrEmpty(email))
            {
                return Challenge(); // force login
            }

            // SITE ADMIN VIEW – pending restaurants
            if (email.Equals(SiteAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                var pendingRestaurants = await _context.Restaurants
                    .Where(r => r.Status == "Pending")
                    .ToListAsync();

                return View("AdminRestaurants", pendingRestaurants);
            }

            // OWNER VIEW

            if (!restaurantId.HasValue)
            {
                // Step 1: show owned restaurants
                var myRestaurants = await _context.Restaurants
                    .Where(r => r.OwnerEmailAddress == email)
                    .ToListAsync();

                return View("OwnerRestaurants", myRestaurants);
            }
            else
            {
                // Step 2: show pending menu items for selected restaurant
                var pendingMenuItems = await _context.MenuItems
                    .Include(m => m.Restaurant)
                    .Where(m => m.RestaurantId == restaurantId.Value && m.Status == "Pending")
                    .ToListAsync();

                ViewBag.RestaurantId = restaurantId.Value;

                return View("OwnerMenuItems", pendingMenuItems);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ApproveAuthorizationFilter]  // our custom filter
        public async Task<IActionResult> Approve(
            string itemType,
            List<int>? restaurantIds,
            List<Guid>? menuItemIds)
        {
            restaurantIds ??= new List<int>();
            menuItemIds ??= new List<Guid>();

            await _dbRepository.ApproveAsync(
                itemType == "restaurant" ? restaurantIds : new List<int>(),
                itemType == "menuItem" ? menuItemIds : new List<Guid>());

            return RedirectToAction("Index");
        }
    }
}
