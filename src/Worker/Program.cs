using Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<JobStarterWorker>();
builder.Services.AddHostedService<ImageWorker>();
var host = builder.Build();
host.Run();