using System;
using Serilog;
using Serilog.Core;

namespace Ricotta.Logging
{
    public class LoggerFactory
    {
        public static Logger Create(string level)
        {
            var loggerConfig = new LoggerConfiguration();
            switch (level.ToLower())
            {
                case "debug":
                    loggerConfig = loggerConfig.MinimumLevel.Debug();
                    break;
                case "information":
                    loggerConfig = loggerConfig.MinimumLevel.Information();
                    break;
                case "warning":
                    loggerConfig = loggerConfig.MinimumLevel.Warning();
                    break;
                case "error":
                    loggerConfig = loggerConfig.MinimumLevel.Error();
                    break;
                case "fatal":
                    loggerConfig = loggerConfig.MinimumLevel.Fatal();
                    break;
                case "verbose":
                    loggerConfig = loggerConfig.MinimumLevel.Verbose();
                    break;
                default:
                    loggerConfig = loggerConfig.MinimumLevel.Warning();
                    break;
            }

            return loggerConfig
                    .WriteTo.LiterateConsole()
                    .CreateLogger();
        }
    }
}