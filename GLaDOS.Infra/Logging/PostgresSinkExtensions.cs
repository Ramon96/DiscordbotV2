using Serilog;
using Serilog.Configuration;

namespace GLaDOS.Infra.Logging;

public static class PostgresSinkExtensions
{
    public static LoggerConfiguration Postgres(
        this LoggerSinkConfiguration sinkConfiguration,
        string connectionString)
    {
        return sinkConfiguration.Sink(new PostgresLogSink(connectionString));
    }
}
