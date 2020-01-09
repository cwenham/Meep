using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using SmartFormat;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using MeepLib.Messages.Compiled;

namespace MeepLib.Filters
{
    /// <summary>
    /// Match on a Regular Expression
    /// </summary>
    [Macro(DefaultProperty = "Expr", Name = "Match", Position = MacroPosition.Upstream)]
    public class Match : AFilter
    {
        /// <summary>
        /// Part of the message to match on
        /// </summary>
        /// <value>The value.</value>
        /// <remarks>Defaults to a JSON serialised version of the message, so make sure your regex Pattern accounts for
        /// this if you don't narrow the scope here first.</remarks>
        public DataSelector Value { get; set; } = "{msg.Value}";

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
        /// Name of a Memory module with RegexMessages to match on
        /// </summary>
        public DataSelector Patterns { get; set; }

        /// <summary>
        /// Optional body of a return message
        /// </summary>
        /// <value>The return.</value>
        /// <remarks>Overrides a matching message with a new message. 
        /// 
        /// <para>Refer to groups captured in the regex with {rgx.} in {Smart.Format} templates.</para>
        /// 
        /// <para>E.G.: {rgx.Groups[0].Value}</para>
        /// 
        /// <para>If null, this module will just return unmodified any message that matches the expression, acting as
        /// a basic filter.</para>
        /// </remarks>
        public DataSelector Return { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string test = await Value.SelectStringAsync(context);

            IEnumerable<Message> _patterns = null;

            if (Patterns is null && _pattern != null)
                _patterns = new List<RegexMessage> {
                    new RegexMessage
                    {
                        Expression = _pattern
                    }
                };
            else
            {
                string dsPatterns = await Patterns.SelectStringAsync(context);
                MeepLib.Outputs.Memorize memory = this.ByName<MeepLib.Outputs.Memorize>(dsPatterns);
                if (memory == null)
                    return new Messages.Exceptions.ExceptionMessage($"Could not find a module called: {dsPatterns}");

                _patterns = memory;
            }

            foreach (var patMsg in _patterns.OfType<RegexMessage>())
            {
                var m = patMsg.Expression.Match(test);
                if (m.Success)
                    if (Return != null)
                        return ThisPassedTheTest(new StringMessage
                        {
                            DerivedFrom = msg,
                            Value = await Return.SelectStringAsync(new MessageContext(msg, this, m))
                        });
                    else
                        return ThisPassedTheTest(msg);

            }

            return ThisFailedTheTest(msg);
        }
    }
}
