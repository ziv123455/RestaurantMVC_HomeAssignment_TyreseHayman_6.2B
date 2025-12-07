using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace DataAccess.Repositories
{
    public class ItemsInMemoryRepository : IItemsRepository
    {
        private readonly IMemoryCache _cache;
        private const string CacheKey = "BulkImportItems";

        public ItemsInMemoryRepository(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Save items into memory (temporary)
        public Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            _cache.Set(CacheKey, items.ToList());
            return Task.CompletedTask;
        }

        // Get items from memory
        public Task<IReadOnlyList<IItemValidating>> GetAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<IItemValidating> items))
            {
                return Task.FromResult<IReadOnlyList<IItemValidating>>(items);
            }

            return Task.FromResult<IReadOnlyList<IItemValidating>>(Array.Empty<IItemValidating>());
        }

        // Clear cached items
        public Task ClearAsync()
        {
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }
    }
}
