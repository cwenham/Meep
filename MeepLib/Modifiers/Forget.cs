using System;
using System.Threading.Tasks;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// Clear a <see cref="MeepLib.Outputs.Memorize"/> module on receipt of a message
    /// </summary>
    /// <remarks>If you're populating a <see cref="MeepLib.Outputs.Memorize"/> module by re-reading a file every time it
    /// changes and want to start from scratch on each re-read rather than fill it up with dupes of older content.
    ///
    /// <para>It's best to put this just downstream (or use the Macro form in the attributes of) a module that returns
    /// a Batch, such as <see cref="Split"/>, so it only wipes the Memory once instead of after every item, or you'll
    /// end up with a Memory that has only one or zero items in it at any time.</para>
    /// </remarks>
    [Macro(Name = "Forget", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Forget : AMessageModule
    {
        /// <summary>
        /// Name of the Memory module to wipe
        /// </summary>
        public DataSelector From { get; set; }

        public async override Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string dsFrom = await From.SelectStringAsync(context);

            MeepLib.Outputs.Memorize mem = this.ByName<MeepLib.Outputs.Memorize>(dsFrom);
            if (mem is null)
            {
                logger.Warn("Could not find a Memory module called: {0}", dsFrom);
                return msg;
            }

            mem.Clear();

            return msg;
        }
    }
}
