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
    /// that don't know how to work with batches.</remarks>
    public class Unbatch : AMessageModule
    {
        /// <summary>
        /// Un-batch the DerivedFrom chain if the incoming message isn't a formal Batch
        /// </summary>
        /// <value></value>
        /// <remarks>If true, we'll walk the DerivedFrom chain and emit them as 
        /// separate messages again if we're not given a message of type Batch. 
        /// Defaults to false.</remarks>
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
