using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reactive.Linq;

using NLog;
using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.Filters
{
    /// <summary>
    /// Only allow messages that are different from the last change
    /// </summary>
    /// <remarks>Identical to &lt;Distinct&gt; except for the filtering function.</remarks>
    [Macro(Name = "DistinctUntilChanged", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class DistinctUntilChanged : Distinct
    {
        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = UpstreamMessaging.DistinctUntilChanged(b => SelectValue(b)).Publish().RefCount();

                return _pipeline;
            }
            protected set
            {
                _pipeline = value;
            }
        }
    }
}
