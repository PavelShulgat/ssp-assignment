using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Shared;

var builder = WebApplication.CreateBuilder(args);

var storage = builder.Configuration.GetConnectionString("Storage")
    ?? "UseDevelopmentStorage=true";

builder.Services.AddSingleton(_ => new QueueServiceClient(
    storage,
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

builder.Services.AddSingleton(_ => new BlobContainerClient(storage, BlobNames.Images));

var app = builder.Build();

app.MapPost("/images", async (QueueServiceClient queues) =>
{
    var queue = queues.GetQueueClient(QueueNames.JobStart);
    await queue.CreateIfNotExistsAsync();

    var jobId = Guid.NewGuid();
    await queue.SendMessageAsync(JsonSerializer.Serialize(new StartJobMessage(jobId)));

    return Results.Accepted($"/images/{jobId}", new { id = jobId });
});

app.MapGet("/images/{id:guid}", async (Guid id, BlobContainerClient container, CancellationToken ct) =>
{
    var urls = new List<string>();

    await foreach (var blob in container.GetBlobsAsync(
        traits: BlobTraits.None,
        states: BlobStates.None,
        prefix: $"{id}/",
        cancellationToken: ct))
    {
        urls.Add(container.GetBlobClient(blob.Name).Uri.ToString());
    }

    return urls.Count == 0
        ? Results.NotFound(new { id, message = "No images for this job" })
        : Results.Ok(new { id, count = urls.Count, images = urls });
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();