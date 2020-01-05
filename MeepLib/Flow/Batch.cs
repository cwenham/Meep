using System;
using System.Linq;
using System.Reactive.Linq;

using MeepLib.MeepLang;

using MM = MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Gather messages into batches
    /// </summary>
    /// <remarks>Operates on the ferry model: the ferry sails either every MaxWait, or when it's full (MaxSize).
    /// 
    /// <para>Its main use is for modules that are batch-aware, such as database and file savers and APIs with
    /// bulk/batch options.</para>
    /// </remarks>
    [Macro(Name = "Batch", DefaultProperty = "MaxSize", Position = MacroPosition.Downstream)]
    public class Batch : AMessageModule
    {
        /// <summary>
        /// Maximum size of a batch message
        /// </summary>
        public int MaxSize { get; set; } = 10;

        /// <summary>
        /// Maximum time before a message is sent
        /// </summary>
        public TimeSpan MaxWait { get; set; } = TimeSpan.FromMinutes(10);

        protected override IObservable<MM.Message> GetMessagingSource()
        {
            return from b in UpstreamMessaging.Buffer(MaxWait, MaxSize)
                   select new MM.Batch
                   {
                       Messages = b.ToList()
                   };
        }
    }
}
