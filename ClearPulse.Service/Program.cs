using ClearPulse.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

var builder = Host.CreateApplicationBuilder(args);

// Ensure relative paths resolve to the exe folder when running as a service

//builder.Host.UseContentRoot(AppContext.BaseDirectory);
builder.Environment.ContentRootPath = AppContext.BaseDirectory;

// Optionally set the service name from environment variable or command line

string? serviceNameFromEnv = Environment.GetEnvironmentVariable("SERVICE_NAME");

string? serviceNameFromArg = args.FirstOrDefault(a => a.StartsWith("--serviceName=", StringComparison.OrdinalIgnoreCase))

                             ?.Split('=', 2)[1];

string? serviceName = serviceNameFromArg ?? serviceNameFromEnv;

if (!string.IsNullOrEmpty(serviceName))

{
    builder.Services.Configure<WindowsServiceLifetimeOptions>(options => options.ServiceName = serviceName);

}
builder.Services.AddWindowsService();

// Logging: console for interactive runs, EventLog for service runs

builder.Logging.ClearProviders();

builder.Logging.AddConsole();

builder.Logging.AddEventLog();

// Register hosted service(s)

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

await host.RunAsync();