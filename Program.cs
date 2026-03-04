using DemoService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DemoService";
});

builder.Services.AddSingleton<RequestProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
try
{
    host.Run();
}
catch (OperationCanceledException)
{
    // Expected during graceful Windows Service shutdown —
    // WindowsServiceLifetime.StopAsync throws when the shutdown
    // cancellation token fires before stop completes.
}
