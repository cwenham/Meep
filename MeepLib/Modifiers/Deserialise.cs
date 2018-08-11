using System;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json;
using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// Deserialise a string or stream to a Meep Message
    /// </summary>
    [Macro(Name = "Deserialise", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Deserialise : AMessageModule
    {
        /// <summary>
        /// What string property of the message to deserialise, in {Smart.Format}
        /// </summary>
        /// <value></value>
        /// <remarks>This is ignored for WebMessages and StreamMessages, so when
        /// using this as a macro on Get, you could put Deserialise="True" or
        /// any other value and it'd still work as expected.</remarks>
        public string From { get; set; } = "{Contents}";

        public override async Task<Message> HandleMessage(Message msg)
        {
            try
            {
                switch (msg)
                {
                    case IStreamMessage streamMsg:
                        return await FromStream(streamMsg.Stream);
                    default:
                        MessageContext context = new MessageContext(msg, this);
                        string data = Smart.Format(From, context);
                        return JsonConvert.DeserializeObject<Message>(data);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when deserialising message: {1}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        private async Task<Message> FromStream(Task<Stream> stream)
        {
            var reader = new StreamReader(await stream);
            return JsonConvert.DeserializeObject<Message>(await reader.ReadToEndAsync());
        }
    }
}
