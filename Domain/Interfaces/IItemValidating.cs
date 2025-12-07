using System.Collections.Generic;

namespace Domain.Interfaces
{
    public interface IItemValidating
    {
        // Who can approve this item (emails)
        List<string> GetValidators();

        // Which partial view should be used to render this item
        string GetCardPartial();
    }
}
