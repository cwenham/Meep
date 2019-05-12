using System;
using System.Linq;
using System.Reactive.Linq;

using MeepLib.MeepLang;

using MM = MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Skip by a number of messages
    /// </summary>
    /// <remarks>This will only have an effect at the beginning of a pipeline's operation until the Skip By
    /// quantity have passed, and then it has no effect on the pipeline until it's restarted.
    /// </remarks>
    [Macro(Name = "Skip", DefaultProperty = "By", Position = MacroPosition.Downstream)]
    public class Skip : AMessageModule
    {
        public int By { get; set; } = 10;

        public override IObservable<MM.Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = from b in UpstreamMessaging.Skip(By)
                                select b;

                return _pipeline;
            }
            protected set
            {
                _pipeline = value;
            }
        }
        private IObservable<MM.Message> _pipeline;
    }
}
