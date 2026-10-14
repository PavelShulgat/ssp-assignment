using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<JobStarterWorker>();

var host = builder.Build();
host.Run();