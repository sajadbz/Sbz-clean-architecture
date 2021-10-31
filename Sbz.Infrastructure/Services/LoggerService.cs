using NLog;
using Sbz.Application.Common.Interfaces;


namespace Sbz.Infrastructure.Services
{
    public class LoggerManager<TController> : ILoggerManager<TController>
    {
        private readonly ILogger _logger;
        public LoggerManager()
        {
            _logger = LogManager.GetCurrentClassLogger();
        }

        public void LogDebug(string message)
        {
            _logger.Debug(MessageBuilder(message));
        }

        public void LogError(string message)
        {
            _logger.Error(MessageBuilder(message));
        }

        public void LogInfo(string message)
        {
            _logger.Info(MessageBuilder(message));
        }

        public void LogWarn(string message)
        {
            _logger.Warn(MessageBuilder(message));
        }
               

        private string MessageBuilder(string message)
        {
            return typeof(TController).FullName + " | " + message;
        }
    }
}
