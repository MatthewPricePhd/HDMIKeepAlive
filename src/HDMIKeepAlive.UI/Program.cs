using HDMIKeepAlive.UI;
using Microsoft.Extensions.Hosting;

using IHost host = AppBootstrapper.CreateHostBuilder(args).Build();
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

CommandLineOptions options;
try
{
    options = CommandLineOptions.Parse(args);
}
catch (ArgumentException ex)
{
    await Console.Error.WriteLineAsync(ex.Message);
    await Console.Out.WriteLineAsync(CommandLineOptions.GetUsage());
    return 2;
}

var runner = new ConsoleHardwareValidationRunner(host, Console.Out);
return await runner.RunAsync(options, cancellation.Token);
