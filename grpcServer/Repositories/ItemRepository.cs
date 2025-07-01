using Clickhouse;
using grpcServer.Entities;

namespace grpcServer.Repositories
{
    public class ItemRepository(IClickHouseClient clickhouseClient): IItemRepository
    {
        private const string TableName = "items";

        // 1. Получить список (с фильтром по IDs)
        public async Task<IEnumerable<Item>> GetItemsAsync(IEnumerable<Guid>? ids = null)
        {
            var whereClause = ids?.Any() == true
                ? $"id IN ({string.Join(", ", ids.Select(id => $"'{id}'"))})"
                : "1=1";

            var sql = $"SELECT id, name, quantity, price FROM {TableName} WHERE {whereClause}";

            var result = await clickhouseClient.QueryAsync(sql);
            return result.Select(row => new Item
            {
                Id = Guid.Parse(row["id"].ToString()!),
                Name = row["name"].ToString()!,
                Quantity = Convert.ToInt32(row["quantity"]),
                Price = Convert.ToDouble(row["price"])
            });
        }

        // 2. Добавить элемент (возвращает Guid нового элемента)
        public async Task<Guid> AddItemAsync(Item item)
        {
            var newId = Guid.NewGuid();
            var row = new Dictionary<string, object>
            {
                ["id"] = newId.ToString(),
                ["name"] = item.Name,
                ["quantity"] = item.Quantity,
                ["price"] = item.Price
            };

            await clickhouseClient.InsertAsync(TableName, row);
            return newId;
        }

        // 3. Удалить элемент по ID
        public async Task<bool> DeleteItemAsync(Guid id)
        {
            try
            {
                await clickhouseClient.DeleteAsync(TableName, $"id = '{id}'");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // 4. Обновить элемент (по всем полям)
        public async Task<Item?> UpdateItemAsync(Item item)
        {
            var sql = $@"
            ALTER TABLE {TableName}
            UPDATE 
                name = '{item.Name.Replace("'", "''")}',
                quantity = {item.Quantity},
                price = {item.Price}
            WHERE id = '{item.Id}'";

            await clickhouseClient.ExecuteAsync(sql);

            return item;
        }
    }
}
