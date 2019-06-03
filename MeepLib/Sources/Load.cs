using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

using NLog;
using SmartFormat;

using MeepLib.Messages;
using MeepLib.MeepLang;
using System.Threading.Tasks;

namespace MeepLib.Sources
{
    /// <summary>
    /// Load a file from disk
    /// </summary>
    [Macro(Name = "Load", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Load : AMessageModule
    {
        /// <summary>
        /// Path in {Smart.Format} for file to load
        /// </summary>
        /// <value>From.</value>
        public string From { get; set; }

        /// <summary>
        /// Max size of file before switching to returning a stream
        /// </summary>
        /// <value>The stream at.</value>
        public long StreamAt { get; set; } = 1024 * 1024; // Default to a MB

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string filePath = Smart.Format(From, context);
                if (File.Exists(filePath))
                    try
                    {
                        FileInfo info = new FileInfo(filePath);

                        if (info.Length < StreamAt)
                            return new StringMessage
                            {
                                DerivedFrom = msg,
                                Value = File.ReadAllText(filePath)
                            };
                        else
                            return new StreamMessage(File.OpenRead(filePath));
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "{0} thrown when loading file: {1}", ex.GetType().Name, ex.Message);
                    }

                return null;
            });

        }
    }
}
