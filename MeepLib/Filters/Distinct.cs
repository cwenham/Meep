using System;
using System.Linq;
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
    [Macro(Name = "Distinct", DefaultProperty = "From", Position = MacroPosition.Upstream)]
    public class Distinct : AMessageModule
    {
        /// <summary>
        /// XPath, JSON Path, RegEx or {Smart.Format}, chosen according to the inbound message type
        /// </summary>
        /// <remarks>Recognises Meep conventions and type prefixes.</remarks>
        public DataSelector From { get; set; } = "{msg.Value}";

        /// <summary>
        /// Fish the distinctable value from each message according to the From template
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected string SelectValue(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            try
            {
                var selection = From.SelectMessageAsync(context);
                selection.Wait();
                if (selection.Result != null)
                    if (selection.Result is Batch)
                        return ((Batch)selection.Result).Messages.FirstOrDefault()?.ToString();
                    else
                        return selection.Result.ToString();

                return null;
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
                    _pipeline = UpstreamMessaging.Distinct(b => SelectValue(b)).Publish().RefCount();

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
