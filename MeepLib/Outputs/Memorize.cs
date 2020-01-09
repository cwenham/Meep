using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using MeepLib.Messages;
using MeepLib.Messages.Exceptions;
using System.Collections;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Store Messages in memory
    /// </summary>
    /// <remarks>Used as a generic addressable collection. E.G.: fill a Memorize module with RegexMessages and use them
    /// in Match.
    ///
    /// <code>
    ///     &lt;Memorize Name="Signatures"&gt;
    ///         &lt;Compile Language="Regex"&gt;
    ///             &lt;Split Forget="Signatures"&gt;
    ///                 &lt;Load&gt;
    ///                     &lt;FileChanges Path="/path/to/data" Pattern="signatures.txt"/&gt;
    ///                 &lt;/Load&gt;
    ///             &lt;/Split&gt;
    ///         &lt;/Compile&gt;
    ///     &lt;/Memorize&gt;
    ///
    ///     &lt;Match Patterns="Signatures"&gt;
    ///         &lt;Split&gt;
    ///             &lt;Load&gt;
    ///                 &lt;FileChanges Path="/path/to/data" Pattern="samples.txt"/&gt;
    ///             &lt;Load&gt;
    ///         &lt;/Split&gt;
    ///     &lt;/Match&gt;
    /// </code>
    ///
    /// <para>If the From attribute is given, Memory will not store inbound Messages, but instead look for a sibling
    /// with the same name and interpret each inbound message as a trigger to emit a stored Message. E.G.: combine with
    /// a Timer to replay stored messages according to a Mode.</para>
    ///
    /// <code>
    ///     &lt;Memorize Name="Fruits"&gt;
    ///         &lt;Split&gt;
    ///             &lt;Load From="fruits.txt"&gt;
    ///         &lt;/Split&gt;
    ///     &lt;/Memorize&gt;
    ///
    ///     &lt;WriteLine From="{msg.Value}" Unbatch="Children"&gt;
    ///         &lt;Memorize From="Fruits"&gt;
    ///             &lt;Timer Interval="00:01:00"/&gt;
    ///         &lt;/Memorize&gt;
    ///     &lt;/WriteLine&gt;
    /// </code>
    ///
    /// <para>For playback, Messages are returned in a Batch wrapper to maintain their original derivation chain along
    /// with the trigger Message. If you're only interested in the payload Message, just add Unbatch="Children" to the
    /// attributes of the downstream module like the example above.</para>
    ///
    /// <para>If reloading the source of data to Memorize, use <see cref="MeepLib.Modifiers.Forget"/> to clear a
    /// Memorize module for each inbound Batch.</para>
    /// 
    /// </remarks>
    public class Memorize : AMessageModule, IEnumerable<Message>
    {
        /// <summary>
        /// The internal collection of messages
        /// </summary>
        protected ConcurrentDictionary<Guid, Message> Messages { get; set; } = new ConcurrentDictionary<Guid, Message>();

        /// <summary>
        /// Just the values, ordered by Message.Created ascending
        /// </summary>
        protected IOrderedEnumerable<Message> orderedAsc = null;

        /// <summary>
        /// Just the values, ordered by Message.Created descending
        /// </summary>
        protected IEnumerable<Message> orderedDesc = null;

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
        private Random rand = new Random();

        public async override Task<Message> HandleMessage(Message msg)
        {
            // Storage mode

            if (From is null)
            {
                if (!Messages.ContainsKey(msg.ID))
                    if (Messages.TryAdd(msg.ID, msg))
                    {
                        // Notes: assuming playback will be more frequent than insertion and it's better to pay the
                        // cost up-front rather than on each trigger in playback mode.
                        orderedAsc = Messages.Values.OrderBy(x => x.CreatedTicks);
                        orderedDesc = orderedAsc.Reverse();
                    }

                return msg;
            }

            // Playback mode

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
                        Messages = sibling.Messages.Values
                    };
                case "Random":
                    int msgIndex = rand.Next(0, Messages.Count);
                    return new Batch
                    {
                        DerivedFrom = msg,
                        Messages = new List<Message> { sibling.Messages.ElementAt(msgIndex).Value }
                    };
                case "DescendingTime":
                    next = NextOrdered(sibling, false);
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
                    next = NextOrdered(sibling, true);
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

        private Message NextOrdered(Memorize from, bool ascending)
        {
            if (lastOffset >= (from.Messages.Count - 1))
                lastOffset = 0;
            else
                System.Threading.Interlocked.Increment(ref lastOffset);

            if (ascending)
                return from.orderedAsc?.ElementAt(lastOffset);
            else
                return from.orderedDesc?.ElementAt(lastOffset);
        }

        public IEnumerator<Message> GetEnumerator()
        {
            return Messages.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Messages.Values.GetEnumerator();
        }

        public void Clear()
        {
            this.Messages?.Clear();
        }
    }
}
