using System;
using System.Text.RegularExpressions;

namespace MeepLib.Messages.Compiled
{
    public class RegexMessage : Message
    {
        /// <summary>
        /// The compiled Regex
        /// </summary>
        public Regex Expression { get; set; }

        public RegexMessage()
        { }

        public RegexMessage(string pattern, Message derivedFrom, string name = null)
        {
            this.Name = name;
            this.Expression = new Regex(pattern, RegexOptions.Compiled);
            this.DerivedFrom = derivedFrom;
        }
    }
}
