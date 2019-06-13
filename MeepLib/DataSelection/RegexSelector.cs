using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using MeepLib;

namespace MeepLib.DataSelection
{
    [DataSelector(Prefix = "RX:")]
    public class RegexSelector : ADataSelector
    {
        public RegexSelector(string template) : base(template)
        {
            this.Template = template;
            r_template = new Regex(template, RegexOptions.Compiled);
        }

        private Regex r_template;

        public override async IAsyncEnumerable<object> Select(MessageContext context)
        {
            MatchCollection matches = await Task.Run<MatchCollection>(() =>
            {
                return r_template.Matches(context.ToString());
            });

            foreach (Match m in matches)
                yield return m.Value;

            yield break;
        }
    }
}
