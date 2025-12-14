using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IItemsRepository
    {
        Task<IReadOnlyList<IItemValidating>> GetAsync();
        Task SaveAsync(IEnumerable<IItemValidating> items);
        Task ClearAsync();
    }
}
