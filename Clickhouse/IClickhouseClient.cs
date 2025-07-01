namespace Clickhouse
{
    public interface IClickHouseClient
    {
        Task ExecuteAsync(string sql);
        Task<IEnumerable<Dictionary<string, object>>> QueryAsync(string sql);
        Task InsertAsync(string tableName, Dictionary<string, object> row);
        Task DeleteAsync(string tableName, string whereClause);
        Task BulkInsertAsync(string tableName, IEnumerable<Dictionary<string, object>> rows);
    }
}
