using HDMIKeepAlive.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using IHost host = AppBootstrapper.CreateHostBuilder(args).Build();

ILoggerFactory loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
ILogger logger = loggerFactory.CreateLogger("HDMIKeepAlive");
logger.LogInformation("HDMIKeepAlive Milestone 0 host initialized.");
