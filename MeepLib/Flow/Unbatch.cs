using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

using MeepLib.MeepLang;

using MM = MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Break a Batch message out into individual messages
    /// </summary>
    /// <remarks>Often book-ended with the Batch module to pass on to modules
    /// that don't know how to work with batches.
    /// 
    /// <para>This can also be used to break-out the DerivedFrom hierarchy by 
    /// setting Family="True".</para>
    /// </remarks>
    [Macro(Name = "Unbatch", DefaultProperty = "Mode", Position = MacroPosition.Upstream)]
    public class Unbatch : AMessageModule
    {
        /// <summary>
        /// Treat the family of "DerivedFrom" messages as the batch
        /// </summary>
        /// <value></value>
        /// <remarks>Usually, a batch is the Message type Batch or one of its
        /// subclasses, which is a collection of messages in an enumerable of 
        /// some sort. But another kind of batch is the ancestory or family of 
        /// messages that are linked together by the DerivedFrom field. Set
        /// this to True to treat a message and its ancestors as the batch,
        /// instead.</remarks>
        public bool Family { get; set; } = false;

        /// <summary>
        /// Mode of unbatching, alternative to "Family"
        /// </summary>
        /// <value>"Family" or "Children"</value>
        /// <remarks>Makes it more intuitive to read when used as a macro, eg:
        /// Unbatch="Family" or Unbatch="Children".</remarks>
        public string Mode
        {
            get 
            {
                if (Family)
                    return "Family";
                else
                    return "Children";
            }
            set
            {
                switch (value.ToLower())
                {
                    case "family":
                        Family = true;
                        break;
                    case "children":
                        Family = false;
                        break;
                    default:
                        break;
                }
            }
        }


        public override IObservable<MM.Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = from b in UpstreamMessaging
                                from m in Constituents(b)
                                select m;

                return _pipeline;
            }
            protected set
            {
                _pipeline = value;
            }
        }
        private IObservable<MM.Message> _pipeline;

        private IEnumerable<MM.Message> Constituents(MM.Message msg)
        {
            MM.Batch bMsg = msg as MM.Batch;
            if (bMsg != null)
                foreach (var m in bMsg.Messages)
                    yield return m;

            if (Family)
                while (msg != null)
                {
                    yield return msg;
                    msg = msg.DerivedFrom;
                }
        }
    }
}
