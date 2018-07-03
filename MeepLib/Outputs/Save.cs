using System;
using System.Threading.Tasks;
using System.IO;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Save message to disk
    /// </summary>
    [Macro(Name = "SaveAs", DefaultProperty = "As", Position = MacroPosition.Downstream)]
    public class Save : AMessageModule
    {
        /// <summary>
        /// Filename and path in {Smart.Format}
        /// </summary>
        /// <value>As.</value>
        public string As { get; set; }

        /// <summary>
        /// Part of the message to save in {Smart.Format}
        /// </summary>
        /// <value>From.</value>
        /// <remarks>Defaults to saving the whole message serialised to JSON</remarks>
        public string From { get; set; } = "{msg.AsJSON}";

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            return await Task.Run<Message>(() =>
            {
                string saveAs = Smart.Format(As, context);
                string content = Smart.Format(From, context);

                try
                {
                    File.WriteAllText(saveAs, content);
                    return new LocalisedResource
                    {
                        DerivedFrom = msg,
                        Local = saveAs,
                        Value = content
                    }; 
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{0} thrown saving to {1}: {2}", ex.GetType().Name, saveAs, ex.Message);
                    return null;
                }
            });
        }
    }
}
