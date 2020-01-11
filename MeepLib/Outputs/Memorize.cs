using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using MeepLib.MeepLang;
using MeepLib.Messages;
using MeepLib.Messages.Exceptions;

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
    /// <para>If reloading the source of data to Memorize, use <see cref="MeepLib.Modifiers.Forget"/> to clear a
    /// Memorize module for each inbound Batch.</para>
    /// 
    /// </remarks>
    [Macro(Name = "Memorize", DefaultProperty = "Name", Position = MacroPosition.Downstream)]    
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

        public async override Task<Message> HandleMessage(Message msg)
        {
            if (!Messages.ContainsKey(msg.ID))
                if (Messages.TryAdd(msg.ID, msg))
                {
                    // Notes: assuming playback will be more frequent than insertion and it's better to pay the
                    // cost up-front rather than on each trigger in playback mode.
                    await Task.Run(() =>
                    {
                        orderedAsc = Messages.Values.OrderBy(x => x.CreatedTicks);
                        orderedDesc = orderedAsc.Reverse();
                    });
                }

            return msg;
        }

        public int Count()
        {
            return Messages.Count();
        }

        public Message NextOrdered(bool ascending, ref int lastOffset)
        {
            if (lastOffset >= (Messages.Count - 1))
                lastOffset = 0;
            else
                System.Threading.Interlocked.Increment(ref lastOffset);

            if (ascending)
                return orderedAsc?.ElementAt(lastOffset);
            else
                return orderedDesc?.ElementAt(lastOffset);
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
