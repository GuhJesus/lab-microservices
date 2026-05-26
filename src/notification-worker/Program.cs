using notification_worker;
using notification_worker.Observability;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWorkerObservability(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
