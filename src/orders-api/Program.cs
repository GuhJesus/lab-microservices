using Microsoft.EntityFrameworkCore;
using orders_api.Caching;
using orders_api.Messaging;
using orders_api.Observability;
using orders_api.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOrdersObservability(builder.Configuration);
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));
builder.Services.AddControllers();

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:Connection"];
});

builder.Services.AddSingleton<IOrderCreatedPublisher, RabbitMqOrderCreatedPublisher>();
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();
builder.Services.AddScoped<IOrderCache, RedisOrderCache>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "orders-api" }));
app.MapControllers();

app.Run();
