using gateway_api.Observability;
using Yarp.ReverseProxy;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("yarp.json", optional: false, reloadOnChange: true);
builder.Services.AddGatewayObservability(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "gateway-api" }));
app.MapReverseProxy();

app.Run();
