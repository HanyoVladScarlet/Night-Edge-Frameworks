using Microsoft.Extensions.Logging;

namespace NightEdgeFrameworks.Core;

public interface INefxLogger
{
    void LogCritical(string message, object[] args = null);
    void LogDebug(string message, object[] args = null);
    void LogError(string message, object[] args = null);
    void LogInformation(string message, object[] args = null);
    void LogTrace(string message, object[] args = null);
    void LogWarning(string message, object[] args = null);
}

public class NefxLogger : INefxLogger
{
    private ILogger _logger;

    public NefxLogger(ILogger logger)
    {
        _logger = logger;
    }

    public void LogInformation(string message, object[] args = default)
    {
        _logger.LogInformation(message, args);
    }

    public void LogError(string message, object[] args = default)
    {
        _logger.LogError(message, args = default);
    }

    public void LogWarning(string message, object[] args = default)
    {
        _logger.LogWarning(message, args);
    }

    public void LogDebug(string message, object[] args = default)
    {
        _logger.LogDebug(message, args);
    }

    public void LogTrace(string message, object[] args = default)
    {
        _logger.LogTrace(message, args);
    }

    public void LogCritical(string message, object[] args = default)
    {
        _logger.LogCritical(message, args);
    }
}

