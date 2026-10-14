using System.Text.Json;
using Azure.Storage.Queues;
using Shared;

namespace Worker;

public class JobStarterWorker : BackgroundService
{
    private readonly ILogger<JobStarterWorker> _logger;
    private readonly QueueClient _queue;

    public JobStarterWorker(ILogger<JobStarterWorker> logger)
    {
        _logger = logger;
        _queue = new QueueClient(
            "UseDevelopmentStorage=true",
            QueueNames.JobStart,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _queue.CreateIfNotExistsAsync(cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _queue.ReceiveMessageAsync(cancellationToken: stoppingToken);
            var message = response.Value;

            if (message is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            var job = JsonSerializer.Deserialize<StartJobMessage>(message.MessageText);
            _logger.LogInformation("Got job {JobId}", job!.JobId);

            await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
        }
    }
}