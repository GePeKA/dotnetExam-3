using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;
using System.Data;

namespace Clickhouse
{
    public class ClickHouseClient(string connectionString) : IClickHouseClient
    {
        private readonly string _connectionString = connectionString;

        public async Task ExecuteAsync(string sql)
        {
            using var conn = new ClickHouseConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<Dictionary<string, object>>> QueryAsync(string sql)
        {
            await using var conn = new ClickHouseConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            using var reader = await cmd.ExecuteReaderAsync();
            var results = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[ToSnakeCase(reader.GetName(i))] = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            return results;
        }

        public async Task InsertAsync(string tableName, Dictionary<string, object> row)
        {
            var columns = row.Keys.Select(ToSnakeCase).ToArray();
            var values = columns.Select(col => $"@{col}").ToArray();

            var sql = $"INSERT INTO {ToSnakeCase(tableName)} ({string.Join(", ", columns)}) VALUES ({string.Join(", ", values)})";

            await using var conn = new ClickHouseConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            foreach (var kvp in row)
            {
                cmd.Parameters.Add(new ClickHouse.Client.ADO.Parameters.ClickHouseDbParameter
                {
                    ParameterName = ToSnakeCase(kvp.Key),
                    Value = kvp.Value!.GetType().IsEnum
                        ? (int)kvp.Value
                        : kvp.Value ?? DBNull.Value
                });
            }

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(string tableName, string whereClause)
        {
            var sql = $"ALTER TABLE {ToSnakeCase(tableName)} DELETE WHERE {whereClause}";

            await using var conn = new ClickHouseConnection(_connectionString);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task BulkInsertAsync(string tableName, IEnumerable<Dictionary<string, object>> rows)
        {
            var rowList = rows.ToList();
            if (rowList.Count == 0)
                return;

            var columnNames = rowList.First().Keys.Select(ToSnakeCase).ToArray();

            var dataTable = new DataTable();
            foreach (var column in columnNames)
            {
                dataTable.Columns.Add(column);
            }

            foreach (var row in rowList)
            {
                var values = columnNames.Select(name => row.TryGetValue(name, out var val) ? val ?? DBNull.Value : DBNull.Value).ToArray();
                dataTable.Rows.Add(values);
            }

            await using var conn = new ClickHouseConnection(_connectionString);
            await conn.OpenAsync();

            var bulkCopy = new ClickHouseBulkCopy(conn)
            {
                DestinationTableName = ToSnakeCase(tableName),
                BatchSize = 10000
            };

            await bulkCopy.InitAsync();
            await bulkCopy.WriteToServerAsync(dataTable, CancellationToken.None);
        }

        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                var ch = input[i];
                if (char.IsUpper(ch))
                {
                    if (i > 0) sb.Append('_');
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return sb.ToString();
        }
    }
}
