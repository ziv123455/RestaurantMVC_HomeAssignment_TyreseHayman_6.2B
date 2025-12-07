using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories
{
    public class ItemsDbRepository : IItemsRepository
    {
        private readonly RestaurantDbContext _context;

        public ItemsDbRepository(RestaurantDbContext context)
        {
            _context = context;
        }

        // Save Restaurants and MenuItems to the database
        public async Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            foreach (var item in items)
            {
                switch (item)
                {
                    case Restaurant r:
                        _context.Restaurants.Add(r);
                        break;

                    case MenuItem m:
                        _context.MenuItems.Add(m);
                        break;
                }
            }

            await _context.SaveChangesAsync();
        }

        // Get all items (both restaurants and menu items)
        public async Task<IReadOnlyList<IItemValidating>> GetAsync()
        {
            var restaurants = await _context.Restaurants
                .ToListAsync<IItemValidating>();

            var menuItems = await _context.MenuItems
                .Include(m => m.Restaurant)
                .ToListAsync<IItemValidating>();

            return restaurants.Concat(menuItems).ToList();
        }

        // For DB repo, ClearAsync doesn't really make sense -> no-op
        public Task ClearAsync()
        {
            return Task.CompletedTask;
        }

        // NEW: Approve restaurants and menu items by id
        public async Task ApproveAsync(IEnumerable<int> restaurantIds, IEnumerable<Guid> menuItemIds)
        {
            if (restaurantIds != null)
            {
                var rIds = restaurantIds.ToList();
                if (rIds.Any())
                {
                    var restaurants = await _context.Restaurants
                        .Where(r => rIds.Contains(r.Id))
                        .ToListAsync();

                    foreach (var r in restaurants)
                    {
                        r.Status = "Approved";
                    }
                }
            }

            if (menuItemIds != null)
            {
                var mIds = menuItemIds.ToList();
                if (mIds.Any())
                {
                    var menuItems = await _context.MenuItems
                        .Where(m => mIds.Contains(m.Id))
                        .ToListAsync();

                    foreach (var m in menuItems)
                    {
                        m.Status = "Approved";
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
