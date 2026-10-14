using System.Text.Json;
using Azure.Storage.Queues;
using Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ => new QueueServiceClient(
    "UseDevelopmentStorage=true",
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

var app = builder.Build();

app.MapPost("/images", async (QueueServiceClient queues) =>
{
    var queue = queues.GetQueueClient(QueueNames.JobStart);
    await queue.CreateIfNotExistsAsync();

    var jobId = Guid.NewGuid();
    await queue.SendMessageAsync(JsonSerializer.Serialize(new StartJobMessage(jobId)));

    return Results.Accepted($"/images/{jobId}", new { id = jobId });
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();