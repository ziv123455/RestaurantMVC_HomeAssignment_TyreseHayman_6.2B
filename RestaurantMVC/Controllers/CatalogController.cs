using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess;
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

        public CatalogController(RestaurantDbContext context)
        {
            _context = context;
        }

        // Everyone (even anonymous) can see the catalog
        [AllowAnonymous]
        public async Task<IActionResult> Index(string type = "restaurants", int? restaurantId = null)
        {
            // Always show only APPROVED items in the public catalog
            IQueryable<Restaurant> restaurantsQuery = _context.Restaurants
                .Where(r => r.Status == "Approved");

            IQueryable<MenuItem> menuItemsQuery = _context.MenuItems
                .Include(m => m.Restaurant)
                .Where(m => m.Status == "Approved");

            // If user is viewing menu items *for a specific restaurant*
            if (string.Equals(type, "menu", StringComparison.OrdinalIgnoreCase) && restaurantId.HasValue)
            {
                menuItemsQuery = menuItemsQuery.Where(m => m.RestaurantId == restaurantId.Value);
            }

            List<IItemValidating> model;
            if (string.Equals(type, "menu", StringComparison.OrdinalIgnoreCase))
            {
                model = await menuItemsQuery.Cast<IItemValidating>().ToListAsync();
            }
            else
            {
                type = "restaurants"; // normalise any other value
                model = await restaurantsQuery.Cast<IItemValidating>().ToListAsync();
            }

            ViewBag.Type = type;
            ViewBag.RestaurantId = restaurantId;

            return View(model);
        }
    }
}
