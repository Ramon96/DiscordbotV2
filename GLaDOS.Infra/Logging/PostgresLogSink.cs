using System.Text.Json;
using System.Threading.Channels;
using Npgsql;
using NpgsqlTypes;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace GLaDOS.Infra.Logging;

/// <summary>
/// Serilog sink that persists log events to a Postgres "logs" table. Events are queued onto a
/// bounded channel and drained by a single background writer that bulk-inserts via COPY, so
/// application threads never block on the database. The table is created on first write, which
/// keeps it out of EF migrations.
/// </summary>
public sealed class PostgresLogSink : ILogEventSink, IDisposable
{
    private const int MaxBatchSize = 100;

    private readonly string _connectionString;
    private readonly Channel<LogEvent> _channel;
    private readonly Task _worker;
    private bool _tableEnsured;

    public PostgresLogSink(string connectionString)
    {
        _connectionString = connectionString;
        _channel = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(5000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
        });
        _worker = Task.Run(ProcessQueueAsync);
    }

    public void Emit(LogEvent logEvent)
    {
        _channel.Writer.TryWrite(logEvent);
    }

    private async Task ProcessQueueAsync()
    {
        var reader = _channel.Reader;
        var batch = new List<LogEvent>(MaxBatchSize);

        while (await reader.WaitToReadAsync())
        {
            batch.Clear();
            while (batch.Count < MaxBatchSize && reader.TryRead(out var logEvent))
            {
                batch.Add(logEvent);
            }

            try
            {
                await WriteBatchAsync(batch);
            }
            catch (Exception exception)
            {
                SelfLog.WriteLine("PostgresLogSink failed to write batch: {0}", exception);
            }
        }
    }

    private async Task WriteBatchAsync(IReadOnlyList<LogEvent> batch)
    {
        if (batch.Count == 0)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await EnsureTableAsync(connection);

        await using var writer = await connection.BeginBinaryImportAsync(
            "COPY logs (timestamp, level, message, exception, source_context, properties) FROM STDIN (FORMAT BINARY)");

        foreach (var logEvent in batch)
        {
            await writer.StartRowAsync();
            await writer.WriteAsync(logEvent.Timestamp, NpgsqlDbType.TimestampTz);
            await writer.WriteAsync(logEvent.Level.ToString(), NpgsqlDbType.Text);
            await writer.WriteAsync(logEvent.RenderMessage(), NpgsqlDbType.Text);
            await WriteNullableTextAsync(writer, logEvent.Exception?.ToString());
            await WriteNullableTextAsync(writer, ReadSourceContext(logEvent));
            await WriteNullableJsonbAsync(writer, SerializeProperties(logEvent));
        }

        await writer.CompleteAsync();
    }

    private async Task EnsureTableAsync(NpgsqlConnection connection)
    {
        if (_tableEnsured)
        {
            return;
        }

        const string ddl = """
            CREATE TABLE IF NOT EXISTS logs (
                id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                timestamp timestamptz NOT NULL,
                level text NOT NULL,
                message text NOT NULL,
                exception text NULL,
                source_context text NULL,
                properties jsonb NULL
            );
            CREATE INDEX IF NOT EXISTS ix_logs_timestamp ON logs (timestamp DESC);
            CREATE INDEX IF NOT EXISTS ix_logs_level ON logs (level);
            """;

        await using var command = new NpgsqlCommand(ddl, connection);
        await command.ExecuteNonQueryAsync();

        _tableEnsured = true;
    }

    private static async Task WriteNullableTextAsync(NpgsqlBinaryImporter writer, string? value)
    {
        if (value is null)
        {
            await writer.WriteNullAsync();
        }
        else
        {
            await writer.WriteAsync(value, NpgsqlDbType.Text);
        }
    }

    private static async Task WriteNullableJsonbAsync(NpgsqlBinaryImporter writer, string? json)
    {
        if (json is null)
        {
            await writer.WriteNullAsync();
        }
        else
        {
            await writer.WriteAsync(json, NpgsqlDbType.Jsonb);
        }
    }

    private static string? ReadSourceContext(LogEvent logEvent)
    {
        return logEvent.Properties.TryGetValue("SourceContext", out var value)
               && value is ScalarValue { Value: string source }
            ? source
            : null;
    }

    private static string? SerializeProperties(LogEvent logEvent)
    {
        if (logEvent.Properties.Count == 0)
        {
            return null;
        }

        var flattened = logEvent.Properties.ToDictionary(
            property => property.Key,
            property => Flatten(property.Value));

        return JsonSerializer.Serialize(flattened);
    }

    private static object? Flatten(LogEventPropertyValue value)
    {
        return value switch
        {
            ScalarValue scalar => scalar.Value,
            _ => value.ToString(),
        };
    }

    public void Dispose()
    {
        _channel.Writer.TryComplete();
        try
        {
            _worker.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Best-effort flush on shutdown.
        }
    }
}
