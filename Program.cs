using DemoService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DemoService";
});

builder.Services.AddSingleton<RequestProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
