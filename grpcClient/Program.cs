using grpcClient.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddGrpc();
builder.Services.AddGrpcClient<ItemGrpcService.ItemGrpcServiceClient>(client =>
{
    client.Address = new Uri("https://localhost:7066");
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
