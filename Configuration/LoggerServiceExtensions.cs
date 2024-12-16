using Serilog;
using Serilog.Exceptions;

namespace backend.Configurations
{
    public static class LoggerServiceExtensions
    {
        public static LoggerConfiguration ConfigureLoggerService(this LoggerConfiguration loggerConfiguration, IConfiguration configuration)
        {
            return loggerConfiguration
                .ReadFrom.Configuration(configuration)
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "Logs/log-.json",
                    rollingInterval: RollingInterval.Day,
                    formatter: new Serilog.Formatting.Compact.RenderedCompactJsonFormatter()
                );
        }
    }
}
