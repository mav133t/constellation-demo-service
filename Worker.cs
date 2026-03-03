namespace DemoService;

/// <summary>
/// Background worker that simulates processing incoming requests.
/// Processes one request every 10 seconds from a generated queue.
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RequestProcessor _processor;
    private int _requestCount = 0;

    public Worker(ILogger<Worker> logger, RequestProcessor processor)
    {
        _logger = logger;
        _processor = processor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DemoService started at {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            _requestCount++;
            var request = GenerateRequest(_requestCount);

            _logger.LogInformation("Processing request #{Count}: Type={Type}, Id={Id}",
                _requestCount, request.Type, request.Id);

            var result = _processor.ProcessRequest(request);

            _logger.LogInformation("Request #{Count} result: {Result}", _requestCount, result);

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private static ServiceRequest GenerateRequest(int sequence)
    {
        // Every 12th request (roughly every 2 minutes) is a "priority" type
        // which will trigger the bug
        if (sequence % 12 == 0)
        {
            return new ServiceRequest
            {
                Id = $"REQ-{sequence:D5}",
                Type = "priority",
                Payload = $"Urgent escalation for ticket TKT-{sequence}"
            };
        }

        // Normal request types that are handled correctly
        var types = new[] { "standard", "batch", "health" };
        var type = types[(sequence - 1) % types.Length];

        return new ServiceRequest
        {
            Id = $"REQ-{sequence:D5}",
            Type = type,
            Payload = $"Routine {type} request #{sequence}"
        };
    }
}
