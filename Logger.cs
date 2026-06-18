using Microsoft.Extensions.Logging;

namespace ProgEsameUniVPM;

/// <summary>
/// Wrapper statico per il sistema di logging. Inizializzato con <see cref="Init"/>,
/// fornisce metodi per ottenere logger tipizzati o per categoria.
/// </summary>
public static class Logger
{
    /// <summary>Factory dei logger, inizializzata all'avvio dell'applicazione.</summary>
    private static ILoggerFactory? _factory;

    /// <summary>
    /// Inizializza il sistema di logging con la factory specificata.
    /// Deve essere chiamato una volta all'avvio, prima di usare <see cref="Get{T}"/> o <see cref="For"/>.
    /// </summary>
    /// <param name="factory">Factory configurata con i provider desiderati.</param>
    public static void Init(ILoggerFactory factory)
    {
        _factory = factory;
    }

    public static void Shutdown()
    {
        _factory!.Dispose();
    }

    /// <summary>
    /// Restituisce un logger tipizzato per la categoria <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Tipo usato come categoria di log.</typeparam>
    /// <returns>Un <see cref="ILogger{T}"/> configurato.</returns>
    /// <exception cref="InvalidOperationException">Se <see cref="Init"/> non è stato chiamato.</exception>
    public static ILogger<T> Get<T>() =>
        _factory?.CreateLogger<T>() ?? throw new InvalidOperationException("Logger non inizializzato. Chiamare Logger.Init() prima.");

    /// <summary>
    /// Restituisce un logger per una categoria testuale specifica.
    /// </summary>
    /// <param name="category">Nome della categoria (es. "GameManager", "Mappa").</param>
    /// <returns>Un <see cref="ILogger"/> configurato.</returns>
    /// <exception cref="InvalidOperationException">Se <see cref="Init"/> non è stato chiamato.</exception>
    public static ILogger For(string category) =>
        _factory?.CreateLogger(category) ?? throw new InvalidOperationException("Logger non inizializzato. Chiamare Logger.Init() prima.");
}

/// <summary>
/// Provider di logging che scrive i messaggi su un file di testo.
/// Implementa <see cref="ILoggerProvider"/> per l'integrazione con Microsoft.Extensions.Logging.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    /// <summary>Writer per il file di log (append con auto-flush).</summary>
    private readonly StreamWriter _writer;
    /// <summary>Livello minimo di log accettato.</summary>
    private readonly LogLevel _minLevel;

    /// <summary>
    /// Crea un nuovo provider che scrive sul file specificato.
    /// </summary>
    /// <param name="path">Percorso del file di log.</param>
    /// <param name="minLevel">Livello minimo di log (default: Debug).</param>
    public FileLoggerProvider(string path, LogLevel minLevel = LogLevel.Debug)
    {
        _minLevel = minLevel;
        _writer = new StreamWriter(path, append: true) { AutoFlush = true };
    }

    /// <summary>
    /// Crea un logger per la categoria specificata.
    /// </summary>
    /// <param name="categoryName">Nome della categoria.</param>
    /// <returns>Un nuovo <see cref="FileLogger"/>.</returns>
    public ILogger CreateLogger(string categoryName) => new FileLogger(categoryName, _writer, _minLevel);

    /// <summary>
    /// Rilascia le risorse del writer su file.
    /// </summary>
    public void Dispose() => _writer.Dispose();
}

/// <summary>
/// Implementazione di <see cref="ILogger"/> che scrive i messaggi formattati su un file di testo.
/// Ogni riga include timestamp, livello, categoria e messaggio.
/// </summary>
public sealed class FileLogger : ILogger
{
    /// <summary>Categoria del logger.</summary>
    private readonly string _category;
    /// <summary>Writer per il file di output.</summary>
    private readonly StreamWriter _writer;
    /// <summary>Livello minimo di log.</summary>
    private readonly LogLevel _minLevel;

    /// <summary>
    /// Crea un nuovo logger per file.
    /// </summary>
    /// <param name="category">Categoria del logger.</param>
    /// <param name="writer">Writer già configurato per il file di log.</param>
    /// <param name="minLevel">Livello minimo di log accettato.</param>
    public FileLogger(string category, StreamWriter writer, LogLevel minLevel)
    {
        _category = category;
        _writer = writer;
        _minLevel = minLevel;
    }

    /// <summary>
    /// Non supporta scope; restituisce sempre <c>null</c>.
    /// </summary>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <summary>
    /// Verifica se il livello di log specificato è abilitato.
    /// </summary>
    /// <param name="logLevel">Livello da verificare.</param>
    /// <returns><c>true</c> se il livello è >= del minimo configurato.</returns>
    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    /// <summary>
    /// Scrive un messaggio di log sul file con formato: [timestamp] [LEVEL] categoria messaggio.
    /// </summary>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var level = logLevel.ToString().ToUpperInvariant().PadRight(5);
        var message = formatter(state, exception);

        _writer.WriteLine($"[{timestamp}] [{level}] {_category,-20} {message}");
    }
}
