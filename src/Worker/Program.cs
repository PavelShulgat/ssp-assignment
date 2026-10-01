using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<JobStartWorker>();

var host = builder.Build();
host.Run();
