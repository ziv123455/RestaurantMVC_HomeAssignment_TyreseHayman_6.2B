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

        public Task<IReadOnlyList<IItemValidating>> GetAsync()
        {
            var list = _cache.Get<List<IItemValidating>>(CacheKey) ?? new List<IItemValidating>();
            return Task.FromResult<IReadOnlyList<IItemValidating>>(list);
        }

        public Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            var list = items?.ToList() ?? new List<IItemValidating>();
            _cache.Set(CacheKey, list);
            return Task.CompletedTask;
        }

        public Task ClearAsync()
        {
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }
    }
}
