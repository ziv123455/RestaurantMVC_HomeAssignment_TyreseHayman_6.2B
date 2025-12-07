using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RestaurantMVC.Controllers
{
    [Authorize]
    public class RequestsController : Controller
    {
        private readonly RestaurantDbContext _context;

        public RequestsController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: /Requests
        // Shows restaurants owned by the logged-in user (by OwnerEmailAddress)
        public async Task<IActionResult> Index()
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(email))
            {
                return Challenge();
            }

            var myRestaurants = await _context.Restaurants
                .Where(r => r.OwnerEmailAddress == email)
                .ToListAsync();

            return View(myRestaurants);
        }

        // GET: /Requests/Details/5
        // Shows details of a single restaurant + its menu items
        public async Task<IActionResult> Details(int id)
        {
            var email = User.FindFirstValue(ClaimTypes.Email) ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(email))
            {
                return Challenge();
            }

            // Make sure user only sees their own restaurant
            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(r => r.Id == id && r.OwnerEmailAddress == email);

            if (restaurant == null)
            {
                return NotFound();
            }

            var menuItems = await _context.MenuItems
                .Where(m => m.RestaurantId == restaurant.Id)
                .ToListAsync();

            ViewBag.MenuItems = menuItems;

            return View(restaurant);
        }
    }
}
