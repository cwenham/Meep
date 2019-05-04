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
    /// Only allow messages with distinct (new) values
    /// </summary>
    /// <remarks></remarks>
    [Macro(Name = "Distinct", DefaultProperty = "From", Position = MacroPosition.Downstream)]
    public class Distinct : AMessageModule
    {
        /// <summary>
        /// The value to check, in {Smart.Format}
        /// </summary>
        public string From { get; set; } = "{msg.AsJSON()}";

        /// <summary>
        /// Fish the distinctable value from each message according to the From template
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected string SelectValue(Message msg)
        {
            try
            {
                MessageContext context = new MessageContext(msg, this);
                return Smart.Format(From, context);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when selecting distinct value: {1}", ex.GetType().Name, ex.Message);
                return null;
            }
        }

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = UpstreamMessaging.Distinct(b => SelectValue(b));

                return _pipeline;
            }
            protected set
            {
                _pipeline = value;
            }
        }
        protected IObservable<Message> _pipeline;
    }
}
