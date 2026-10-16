using System.Text.Json;
using Azure.Storage.Queues;
using Shared;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Worker;

public class ImageWorker : BackgroundService
{
    private readonly ILogger<ImageWorker> _logger;
    private readonly QueueClient _queue;

    private readonly HttpClient _http = new();

    private readonly BlobContainerClient _container;

    public ImageWorker(ILogger<ImageWorker> logger)
    {
        _logger = logger;
        _queue = new QueueClient("UseDevelopmentStorage=true", QueueNames.ImageTasks,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        _container = new BlobContainerClient("UseDevelopmentStorage=true", BlobNames.Images);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _queue.CreateIfNotExistsAsync(cancellationToken: ct);
        await _container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: ct);

        while (!ct.IsCancellationRequested)
        {
            var message = (await _queue.ReceiveMessageAsync(cancellationToken: ct)).Value;

            if (message is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                continue;
            }

            try
            {
                var task = JsonSerializer.Deserialize<ImageTaskMessage>(message.MessageText)!;

                var source = await _http.GetByteArrayAsync("https://picsum.photos/800/600", ct);
                var composed = ImageComposer.Compose(source, task);
                var blob = _container.GetBlobClient($"{task.JobId}/{task.StationId}.png");

                using var stream = new MemoryStream(composed);
                await blob.UploadAsync(stream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = "image/png" }
                }, ct);

                _logger.LogInformation("Uploaded {Name} to {Uri}", task.StationName, blob.Uri);

                await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process image task {MessageId}", message.MessageId);
            }
        }
    }
}