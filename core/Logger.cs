using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace IC10_Extender
{
    public interface ILogger
    {
        void Error(string message);
        void Warning(string message);
        void Info(string message);
        void Debug(string message);
    }

    public enum LogLevel
    {
        Error, Warning, Info, Debug
    }

    public class FallbackLogger : ILogger
    {
        private static readonly ConsoleFallbackLogger Console = new ConsoleFallbackLogger();
        private static readonly UnityFallbackLogger Unity = new UnityFallbackLogger();
        private Func<ILogger> LogSource;
        public LogLevel Level { get; set; }

        public FallbackLogger(Func<ILogger> getter, LogLevel level)
        {
            LogSource = getter;
            Level = level;
        }

        public FallbackLogger(Func<ManualLogSource> getter, LogLevel level)
        {
            LogSource = () =>
            {
                var manual = getter();
                if (manual != null)
                {
                    var logger = new ManualLogSourceLogger(manual);
                    LogSource = () => logger;
                    return logger;
                }
                return Unity;
            };
            Level = level;
        }

        public bool TryLog(ILogger logger, string message, LogLevel messageLevel)
        {
            if (logger == null) return false;
            try
            {
                if (messageLevel <= Level)
                {
                    switch (messageLevel)
                    {
                        case LogLevel.Error: logger.Error(message); break;
                        case LogLevel.Warning: logger.Warning(message); break;
                        case LogLevel.Info: logger.Info(message); break;
                        case LogLevel.Debug: logger.Debug(message); break;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // LogSource() reaches into BepInEx/Unity and throws outside the game player. Log() invokes
        // it as an argument, outside TryLog's try, so swallow here to let the chain fall back.
        private ILogger TryResolveSource()
        {
            try
            {
                return LogSource();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Log(string message, LogLevel messageLevel)
        {
            if(
                TryLog(TryResolveSource(), message, messageLevel)
                || TryLog(Unity, message, messageLevel)
                || TryLog(Console, message, messageLevel))
            {
                return;
            }
            //since console is a fallback this *should* never be possible to reach, but you never know
            throw new InvalidOperationException("Failed to log message: " + message);
        }

        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public void Error(string message)
        {
            Log(message, LogLevel.Error);
        }

        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }

        public void Warning(string message)
        {
            Log(message, LogLevel.Warning);
        }
    }

    internal class ConsoleFallbackLogger : ILogger
    {
        public void Debug(string message)
        {
            Console.WriteLine("Debug: " + message);
        }

        public void Error(string message)
        {
            Console.WriteLine("Error: " + message);
        }

        public void Info(string message)
        {
            Console.WriteLine("Info: " + message);
        }

        public void Warning(string message)
        {
            Console.WriteLine("Warning: " + message);
        }
    }

    internal class UnityFallbackLogger : ILogger
    {
        public void Debug(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }

        public void Info(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }
    }

    public class  ManualLogSourceLogger : ILogger
    {
        private ManualLogSource src;

        public ManualLogSourceLogger(ManualLogSource src)
        {
            this.src = src ?? throw new ArgumentNullException(nameof(src));
        }

        public void Debug(string message)
        {
            src.LogDebug(message);
        }

        public void Error(string message)
        {
            src.LogError(message);
        }

        public void Info(string message)
        {
            src.LogInfo(message);
        }

        public void Warning(string message)
        {
            src.LogWarning(message);
        }
    }
}
