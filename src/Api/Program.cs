using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Shared;
using Azure.Storage.Blobs.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(_ => new QueueServiceClient(
    "UseDevelopmentStorage=true",
    new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));

builder.Services.AddSingleton(_ =>
    new BlobContainerClient("UseDevelopmentStorage=true", BlobNames.Images));

var app = builder.Build();

app.MapPost("/images", async (QueueServiceClient queues) =>
{
    var queue = queues.GetQueueClient(QueueNames.JobStart);
    await queue.CreateIfNotExistsAsync();

    var jobId = Guid.NewGuid();
    await queue.SendMessageAsync(JsonSerializer.Serialize(new StartJobMessage(jobId)));

    return Results.Accepted($"/images/{jobId}", new { id = jobId });
});

app.MapGet("/images/{id:guid}", async (Guid id, BlobContainerClient container) =>
{
    var urls = new List<string>();

    await foreach (var blob in container.GetBlobsAsync(
    traits: BlobTraits.None,
    states: BlobStates.None,
    prefix: $"{id}/",
    cancellationToken: default))
        urls.Add(container.GetBlobClient(blob.Name).Uri.ToString());

    return urls.Count == 0
        ? Results.NotFound(new { id, message = "No images for this job" })
        : Results.Ok(new { id, count = urls.Count, images = urls });
});

app.MapGet("/health", () => Results.Ok("ok"));

app.Run();