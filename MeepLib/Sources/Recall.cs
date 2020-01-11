using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using MeepLib.Outputs;
using MeepLib.Messages;
using MeepLib.Messages.Exceptions;

namespace MeepLib.Sources
{
    /// <summary>
    /// Recall Messages stored by <see cref="MeepLib.Outputs.Memorize"/>
    /// </summary>
    /// <remarks>
    ///
    /// <para>Looks for a sibling with the same name as <see cref="From"/> and interpret each inbound message as a
    /// trigger to emit a stored Message. E.G.: combine with a Timer to replay stored messages according to a Mode.</para>
    ///
    /// <code>
    ///     &lt;Memorize Name="Fruits"&gt;
    ///         &lt;Split&gt;
    ///             &lt;Load From="fruits.txt"&gt;
    ///         &lt;/Split&gt;
    ///     &lt;/Memorize&gt;
    ///
    ///     &lt;WriteLine From="{msg.Value}" Unbatch="Children"&gt;
    ///         &lt;Recall From="Fruits"&gt;
    ///             &lt;Timer Interval="00:01:00"/&gt;
    ///         &lt;/Recall&gt;
    ///     &lt;/WriteLine&gt;
    /// </code>
    ///
    /// <para>For playback, Messages are returned in a Batch wrapper to maintain their original derivation chain along
    /// with the trigger Message. If you're only interested in the payload Message, just add Unbatch="Children" to the
    /// attributes of the downstream module like the example above.</para>
    /// 
    /// </remarks>
    public class Recall : AMessageModule
    {
        /// <summary>
        /// Name of sibling Memory module to read from when replaying Messages
        /// </summary>
        public DataSelector From { get; set; }

        /// <summary>
        /// When replaying messages From a sibling's store, what mode shall we use to cycle through them all?
        /// </summary>
        /// <remarks>Supported modes are:
        ///
        /// <list type="number">
        /// <item>AscendingTime (default) - Ordered by Message.Created</item>
        /// <item>DescendingTime - Ordered by Message.Created</item>
        /// <item>Random - Randomly chosen Message</item>
        /// <item>Batch - All Messages in a BatchMessage</item>
        /// </list>
        ///
        /// </remarks>
        public DataSelector Mode { get; set; } = "AscendingTime";

        /// <summary>
        /// When replaying messages in Ascending/Descending Modes, what was the last one we emitted?
        /// </summary>
        private int lastOffset = -1;

        /// <summary>
        /// RNG for Random playback Mode
        /// </summary>
        private System.Random rand = new System.Random();

        public async override Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string dsFrom = await From.SelectStringAsync(context);
            var sibling = this.ByName<Memorize>(dsFrom);

            if (sibling is null)
                return new ExceptionMessage
                {
                    Exception = new Exception($"Could not find a Memory module called: {dsFrom}")
                };

            Message next = null;
            string dsMode = await Mode.SelectStringAsync(context);
            switch (dsMode)
            {
                case "Batch":
                    return new Batch
                    {
                        DerivedFrom = msg,
                        Messages = sibling
                    };
                case "Random":
                    int msgIndex = rand.Next(0, sibling.Count());
                    return new Batch
                    {
                        DerivedFrom = msg,
                        Messages = new List<Message> { sibling.ElementAt(msgIndex) }
                    };
                case "DescendingTime":
                    next = sibling.NextOrdered(false, ref lastOffset);
                    if (next is null)
                        return null;
                    else
                        return new Batch
                        {
                            DerivedFrom = msg,
                            Messages = new List<Message> { next }
                        };
                case "AscendingTime":
                default:
                    next = sibling.NextOrdered(true, ref lastOffset);
                    if (next is null)
                        return null;
                    else
                        return new Batch
                        {
                            DerivedFrom = msg,
                            Messages = new List<Message> { next }
                        };
            }

        }
    }
}
