using System;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Write to the console
    /// </summary>
    public class WriteLine : AMessageModule
    {
        /// <summary>
        /// String data to write, in {Smart.Format}
        /// </summary>
        /// <value></value>
        /// <remarks>Defaults to message serialised as JSON.</remarks>
        public string From { get; set; } = "{msg.AsJSON}";

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string line = Smart.Format(From, context);

                Console.WriteLine(line);

                return msg;
            });
        }
    }
}
