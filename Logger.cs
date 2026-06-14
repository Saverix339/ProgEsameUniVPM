using Microsoft.Extensions.Logging;

namespace ProgEsameUniVPM;

public static class Logger
{
    private static ILoggerFactory? _factory;

    public static void Init(ILoggerFactory factory)
    {
        _factory = factory;
    }

    public static ILogger<T> Get<T>() =>
        _factory?.CreateLogger<T>() ?? throw new InvalidOperationException("Logger non inizializzato. Chiamare Logger.Init() prima.");

    public static ILogger For(string category) =>
        _factory?.CreateLogger(category) ?? throw new InvalidOperationException("Logger non inizializzato. Chiamare Logger.Init() prima.");
}

public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly LogLevel _minLevel;

    public FileLoggerProvider(string path, LogLevel minLevel = LogLevel.Debug)
    {
        _minLevel = minLevel;
        _writer = new StreamWriter(path, append: true) { AutoFlush = true };
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, _minLevel);

    public void Dispose() => _writer.Dispose();
}

public sealed class FileLogger : ILogger
{
    private readonly string _category;
    private readonly StreamWriter _writer;
    private readonly LogLevel _minLevel;

    public FileLogger(string category, StreamWriter writer, LogLevel minLevel)
    {
        _category = category;
        _writer = writer;
        _minLevel = minLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var level = logLevel.ToString().ToUpperInvariant().PadRight(5);
        var message = formatter(state, exception);

        _writer.WriteLine($"[{timestamp}] [{level}] {_category,-20} {message}");
    }
}
