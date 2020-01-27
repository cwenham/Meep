using System;
using System.Threading.Tasks;

using NLog;
using SmartFormat;

using MeepLib.Messages;
using MeepLib.MeepLang;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Write to a logger
    /// </summary>
    [Macro(Name = "Log", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Log : AMessageModule
    {
        /// <summary>
        /// Log message
        /// </summary>
        public DataSelector From { get; set; }

        /// <summary>
        /// Log level, must evaluate to one of "TRACE", "DEBUG", "INFO", "WARN", "ERROR" or "FATAL", case insensitive
        /// </summary>
        /// <remarks>Defaults to "INFO".</remarks>
        public DataSelector Level { get; set; } = "INFO";

        /// <summary>
        /// Optional name of logger, if not the default
        /// </summary>
        public DataSelector Logger { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string dsFrom = await From.SelectStringAsync(context);
            string dsLevel = await Level.SelectStringAsync(context);
            LogLevel _level = LogLevel.FromString(dsLevel);
            string dsLogger = null;
            if (Logger != null)
                dsLogger = await Logger.SelectStringAsync(context);

            NLog.Logger _logger = dsLogger != null
                            ? LogManager.GetLogger(dsLogger)
                            : LogManager.GetCurrentClassLogger();

            _logger.Log(_level, dsFrom);

            return msg;
        }
    }
}
