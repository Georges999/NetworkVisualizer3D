using System;
using NetworkVisualizer3D.Core.Interfaces;

namespace NetworkVisualizer3D.Core.Services
{
    /// <summary>
    /// Simple console logger implementation
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public void LogTrace(string message)
        {
            WriteLog("TRACE", message, ConsoleColor.Gray);
        }

        public void LogDebug(string message)
        {
            WriteLog("DEBUG", message, ConsoleColor.Gray);
        }

        public void LogInformation(string message)
        {
            WriteLog("INFO", message, ConsoleColor.White);
        }

        public void LogWarning(string message)
        {
            WriteLog("WARN", message, ConsoleColor.Yellow);
        }

        public void LogError(string message)
        {
            WriteLog("ERROR", message, ConsoleColor.Red);
        }

        public void LogCritical(string message)
        {
            WriteLog("CRITICAL", message, ConsoleColor.Magenta);
        }

        private void WriteLog(string level, string message, ConsoleColor color)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var originalColor = Console.ForegroundColor;
            
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"[{timestamp}] ");
                
                Console.ForegroundColor = color;
                Console.Write($"[{level}] ");
                
                Console.ForegroundColor = originalColor;
                Console.WriteLine(message);
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
    }
} 