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
