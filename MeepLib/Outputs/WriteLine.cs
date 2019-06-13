using System;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;
using MeepLib.MeepLang;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Write to the console
    /// </summary>
    [Macro(Name="WriteLine", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class WriteLine : AMessageModule
    {
        /// <summary>
        /// XPath, JSON Path, RegEx or {Smart.Format}, chosen according to the inbound message type
        /// </summary>
        /// <remarks>Recognises Meep conventions and type prefixes.</remarks>
        public DataSelector From { get; set; } = "{msg.AsJSON}";

        public override async Task<Message> HandleMessage(Message msg)
        {
            var enumerator = From.Select(new MessageContext(msg, this)).GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
                Console.WriteLine(enumerator.Current.ToString());

            return msg;
        }
    }
}
