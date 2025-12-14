using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess;
using Domain.Interfaces;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories
{
    public class ItemsDbRepository : IItemsRepository
    {
        private readonly RestaurantDbContext _db;

        public ItemsDbRepository(RestaurantDbContext db)
        {
            _db = db;
        }

        // Not really used by bulk import, but must exist
        public async Task<IReadOnlyList<IItemValidating>> GetAsync()
        {
            var restaurants = await _db.Restaurants.ToListAsync<IItemValidating>();
            var menuItems = await _db.MenuItems
                                     .Include(m => m.Restaurant)
                                     .ToListAsync<IItemValidating>();

            return restaurants.Concat(menuItems).ToList();
        }

        public async Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            var restaurants = items.OfType<Restaurant>().ToList();
            var menuItems = items.OfType<MenuItem>().ToList();

            if (restaurants.Any())
            {
                await _db.Restaurants.AddRangeAsync(restaurants);
            }

            if (menuItems.Any())
            {
                await _db.MenuItems.AddRangeAsync(menuItems);
            }

            await _db.SaveChangesAsync();
        }

        public Task ClearAsync() => Task.CompletedTask;
    }
}
