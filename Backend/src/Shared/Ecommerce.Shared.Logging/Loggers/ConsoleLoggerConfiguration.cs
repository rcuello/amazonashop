using Serilog.Events;

namespace Ecommerce.Shared.Logging.Loggers;

public class ConsoleLoggerConfiguration
{
    public bool Enabled { get; set; } = false;
    public LogEventLevel MinimumLevel { get; set; }
}
