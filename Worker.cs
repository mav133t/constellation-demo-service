using System.Diagnostics;

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
    private const string EventLogSource = "DemoService";

    public Worker(ILogger<Worker> logger, RequestProcessor processor)
    {
        _logger = logger;
        _processor = processor;

        // Ensure event log source exists
        try
        {
            if (!EventLog.SourceExists(EventLogSource))
                EventLog.CreateEventSource(EventLogSource, "Application");
        }
        catch { /* May need admin for first run */ }
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

            try
            {
                var result = _processor.ProcessRequest(request);
                _logger.LogInformation("Request #{Count} result: {Result}", _requestCount, result);
            }
            catch (Exception ex)
            {
                // Write full stack trace to Windows Event Log so the agent can find it
                var message = $"DemoService crashed processing request {request.Id} (Type={request.Type})\n" +
                              $"Customer: {request.CustomerName} ({request.CustomerEmail})\n" +
                              $"Payment: {request.PaymentCard}\n" +
                              $"SSN: {request.SSN}\n\n" +
                              $"Exception: {ex.GetType().FullName}: {ex.Message}\n\n" +
                              $"Stack Trace:\n{ex.StackTrace}";

                try { EventLog.WriteEntry(EventLogSource, message, EventLogEntryType.Error, 1000); }
                catch { /* best effort */ }

                _logger.LogCritical(ex, "Fatal: unhandled exception processing request {Id}", request.Id);

                // Crash the process — this is the demo scenario
                Environment.FailFast($"DemoService fatal error: {ex.Message}", ex);
            }

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
                Payload = $"Urgent escalation for ticket TKT-{sequence}",
                CustomerName = "Jane Doe",
                CustomerEmail = "jane.doe@contoso.com",
                PaymentCard = "4111-1111-1111-1111",
                SSN = "078-05-1120"
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
