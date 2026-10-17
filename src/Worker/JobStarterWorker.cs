using System.Text.Json;
using Azure.Storage.Queues;
using Shared;

namespace Worker;

public class JobStarterWorker : BackgroundService
{
    private readonly ILogger<JobStarterWorker> _logger;
    private readonly QueueClient _startQueue;
    private readonly QueueClient _taskQueue;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

    public JobStarterWorker(ILogger<JobStarterWorker> logger, IConfiguration config)
    {
        _logger = logger;

        var storage = config.GetConnectionString("Storage") ?? "UseDevelopmentStorage=true";
        var options = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };

        _startQueue = new QueueClient(storage, QueueNames.JobStart, options);
        _taskQueue = new QueueClient(storage, QueueNames.ImageTasks, options);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _startQueue.CreateIfNotExistsAsync(cancellationToken: ct);
        await _taskQueue.CreateIfNotExistsAsync(cancellationToken: ct);

        while (!ct.IsCancellationRequested)
        {
            var message = (await _startQueue.ReceiveMessageAsync(cancellationToken: ct)).Value;

            if (message is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
                continue;
            }

            try
            {
                var job = JsonSerializer.Deserialize<StartJobMessage>(message.MessageText)!;
                await FanOutAsync(job.JobId, ct);
                await _startQueue.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process start message {MessageId}", message.MessageId);
            }
        }
    }

    private async Task FanOutAsync(Guid jobId, CancellationToken ct)
    {
        var json = await _http.GetStringAsync("https://data.buienradar.nl/2.0/feed/json", ct);
        var feed = JsonSerializer.Deserialize<BuienradarFeed>(json)!;
        var stations = feed.Actual.StationMeasurements;

        _logger.LogInformation("Job {JobId}: fanning out {Count} stations", jobId, stations.Count);

        foreach (var station in stations)
        {
            var name = station.StationName.Replace("Meetstation ", "");
            var task = new ImageTaskMessage(
                jobId, station.StationId, name, station.Temperature, station.WeatherDescription);

            await _taskQueue.SendMessageAsync(JsonSerializer.Serialize(task), ct);
        }
    }
}