using Clickhouse;
using grpcServer.Repositories;
using grpcServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IClickHouseClient>(provider =>
    new ClickHouseClient(builder.Configuration.GetConnectionString("ClickHouse")!));

builder.Services.AddScoped<IItemRepository, ItemRepository>();

builder.Services.AddGrpc();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var clickHouseClient = scope.ServiceProvider.GetRequiredService<IClickHouseClient>();

    const string createDbSql = @"
        CREATE DATABASE IF NOT EXISTS dotnetExam
        ENGINE = Atomic";

    await clickHouseClient.ExecuteAsync(createDbSql);
    Console.WriteLine("Database 'dotnetExam' initialized (if not exists)");

    const string createTableSql = @"
        CREATE TABLE IF NOT EXISTS dotnetExam.items
        (
            id String,
            name String,
            quantity Int32,
            price Float64
        )
        ENGINE = MergeTree()
        ORDER BY (id)";

    await clickHouseClient.ExecuteAsync(createTableSql);
    Console.WriteLine("Table 'items' initialized (if not exists)");
}

app.MapGrpcService<ItemService>();

app.Run();
