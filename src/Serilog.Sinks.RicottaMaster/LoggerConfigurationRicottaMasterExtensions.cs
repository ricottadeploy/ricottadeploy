using Serilog.Debugging;

namespace Serilog
{
    using System;
    using System.IO;
    using Serilog.Configuration;
    using Serilog.Events;
    using Ricotta.Transport;
    using Serilog.Sinks.RicottaMaster;
    using Ricotta.Serialization;

    public static class LoggerConfigurationRicottaMasterExtensions
    {
        public static LoggerConfiguration RicottaMaster(
           this LoggerSinkConfiguration loggerConfiguration,
           AppClient appClient,
           string agentId,
           string jobId,
           LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
           IFormatProvider formatProvider = null,
           bool storeTimestampInUtc = false,
           TimeSpan? retentionPeriod = null)
        {
            if (loggerConfiguration == null)
            {
                SelfLog.WriteLine("Logger configuration is null");
                throw new ArgumentNullException(nameof(loggerConfiguration));
            }
            return loggerConfiguration.Sink(new RicottaMasterSink(appClient, agentId, jobId));
        }
    }
}
