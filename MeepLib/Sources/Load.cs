using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Xml.Serialization;
using System.IO.Pipelines;

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
    [XmlRoot(ElementName = "Load", Namespace = "http://meep.example.com/Meep/V1")]
    [Macro(Name = "Load", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Load : AMessageModule
    {
        /// <summary>
        /// Path in {Smart.Format} for file to load
        /// </summary>
        /// <value>From.</value>
        [XmlAttribute]
        public string From { get; set; }

        /// <summary>
        /// Max size of file before switching to returning a stream
        /// </summary>
        /// <value>The stream at.</value>
        [XmlAttribute]
        public long StreamAt { get; set; } = 1024 * 1024; // Default to a MB

        public override async Task<Message> HandleMessage(Message msg)
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
                            Value = await File.ReadAllTextAsync(filePath)
                        };
                    else
                        return new StreamMessage
                        {
                            Stream = Task.Run<Stream>(() => File.OpenRead(filePath))
                        };
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{0} thrown when loading file: {1}", ex.GetType().Name, ex.Message);
                }

            return null;
        }
    }
}
