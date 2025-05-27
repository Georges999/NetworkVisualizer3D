namespace NetworkVisualizer3D.Core.Interfaces
{
    /// <summary>
    /// Simple logging interface
    /// </summary>
    public interface ILogger
    {
        void LogTrace(string message);
        void LogDebug(string message);
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogCritical(string message);
    }
} 