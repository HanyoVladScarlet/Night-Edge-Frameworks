using NightEdgeFrameworks.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NightEdgeFrameworks;

var host = Host.CreateDefaultBuilder(args)
  .ConfigureLogging(loggingBuilder =>
  {
    loggingBuilder.AddConsole(options =>
    {
      options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss.fff] ";
    });
  })
  .ConfigureServices(services =>
    {
      services.AddNefxGameEngine(builer =>
      {
        builer.UseDefaultServices();
      });
      // services.AddSingleton<EventPool>();
      // services.AddSingleton<Ticker>();
      // services.AddSingleton<NefxGameEngine>();
    }
).Build();

var engine = host.Services.GetService<INefxGameEngine>();
// engine.StartAsync(CancellationToken.None);

var eventPool = host.Services.GetService<EventPool>();
var logger = host.Services.GetService<ILogger<Program>>();
eventPool?.AddLoopEvent(0.2, 0.2, () => logger.LogInformation("Hello World!"));

logger.LogInformation(host.GetType().FullName);

await host.RunAsync();