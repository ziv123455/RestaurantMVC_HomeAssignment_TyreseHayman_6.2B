using System.Collections.Generic;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IItemsRepository
    {
        Task SaveAsync(IEnumerable<IItemValidating> items);
        Task<IReadOnlyList<IItemValidating>> GetAsync();
        Task ClearAsync();
    }
}
