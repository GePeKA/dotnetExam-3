using grpcServer.Entities;

namespace grpcServer.Repositories
{
    public interface IItemRepository
    {
        Task<IEnumerable<Item>> GetItemsAsync(IEnumerable<Guid>? ids = null);
        Task<Guid> AddItemAsync(Item item);
        Task<bool> DeleteItemAsync(Guid id);
        Task<Item?> UpdateItemAsync(Item item);
    }
}
