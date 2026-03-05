namespace DemoService;

/// <summary>
/// Processes incoming service requests by routing them to the appropriate handler.
/// Each request type maps to a handler function that performs the work.
/// </summary>
public class RequestProcessor
{
    private readonly ILogger<RequestProcessor> _logger;
    private readonly Dictionary<string, Func<ServiceRequest, string>> _handlers;

    public RequestProcessor(ILogger<RequestProcessor> logger)
    {
        _logger = logger;
        _handlers = new Dictionary<string, Func<ServiceRequest, string>>
        {
            ["standard"] = HandleStandard,
            ["batch"] = HandleBatch,
            ["health"] = HandleHealth,
            // BUG: No handler registered for "priority" request type.
            // When a priority request arrives, the dictionary lookup on line 38
            // throws KeyNotFoundException, crashing the service.
        };
    }

    /// <summary>
    /// Routes a request to the appropriate handler based on its Type field.
    /// </summary>
    public string ProcessRequest(ServiceRequest request)
    {
        _logger.LogDebug("Routing request {Id} of type '{Type}'", request.Id, request.Type);

        // BUG: Direct dictionary indexer throws KeyNotFoundException
        // if request.Type is not in the handlers dictionary.
        // Fix: Use TryGetValue to safely check before accessing.
        var handler = _handlers[request.Type];
        var result = handler(request);

        _logger.LogInformation("Request {Id} processed successfully: {Result}", request.Id, result);
        return result;
    }

    private string HandleStandard(ServiceRequest request)
    {
        // Simulate standard processing
        Thread.Sleep(50);
        return $"Standard processing complete for {request.Id}";
    }

    private string HandleBatch(ServiceRequest request)
    {
        // Simulate batch processing
        Thread.Sleep(100);
        return $"Batch processing complete for {request.Id}";
    }

    private string HandleHealth(ServiceRequest request)
    {
        // Simulate health check
        return $"Health check OK — system nominal at {DateTime.UtcNow:O}";
    }
}

/// <summary>
/// Represents an incoming request to be processed by the service.
/// </summary>
public class ServiceRequest
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public string? Payload { get; set; }

    // Simulated customer data — demonstrates why crash pipelines
    // must not leak service-side crash artifacts containing customer content.
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? PaymentCard { get; set; }
    public string? SSN { get; set; }
}
