using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using SmartFormat;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Filters
{
    [Macro(DefaultProperty = "Expr", Name = "Pattern", Position = MacroPosition.Upstream)]
    public class Match : AMessageModule
    {
        /// <summary>
        /// Part of the message to match on, in {Smart.Format}
        /// </summary>
        /// <value>The value.</value>
        /// <remarks>Defaults to a JSON serialised version of the message, so
        /// make sure your regex Pattern accounts for this if you don't narrow
        /// the scope here first.</remarks>
        public string Value { get; set; } = "{msg.AsJSON}";

        /// <summary>
        /// Regex pattern to match with
        /// </summary>
        /// <value>The pattern.</value>
        public string Pattern
        {
            get
            {
                return _pattern.ToString();
            }
            set
            {
                _pattern = new Regex(value, RegexOptions.Compiled);
            }
        }
        private Regex _pattern;

        /// <summary>
        /// Optional body of a return message in {Smart.Format} 
        /// </summary>
        /// <value>The return.</value>
        /// <remarks>Overrides a matching message with a new message. 
        /// 
        /// <para>Refer to groups captured in the regex with {rgx.} in your
        /// template.</para>
        /// 
        /// <para>E.G.: {rgx.Groups[0].Value}</para>
        /// 
        /// <para>If null, this module will just return unmodified any message 
        /// that matches the expression, acting as a basic filter.</para>
        /// </remarks>
        public string Return { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string test = Smart.Format(Value, context);

                var m = _pattern.Match(test);
                if (m.Success)
                    if (!String.IsNullOrWhiteSpace(Return))
                        return new StringMessage
                        {
                            DerivedFrom = msg,
                            Value = Smart.Format(Return, new MessageContext(msg, this, m))
                        };
                    else
                        return msg;

                return null;
            });
        }
    }
}
