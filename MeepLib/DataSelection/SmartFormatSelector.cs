using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

using SmartFormat;
using SmartFormat.Core.Parsing;
using SmartFormat.Core.Formatting;

using MeepLib;

namespace MeepLib.DataSelection
{
    [DataSelector(Prefix = "SF:")]
    public class SmartFormatSelector : ADataSelector, IParameterisable
    {
        public SmartFormatSelector(string template) : base(template)
        {
            this.Template = template;
            Smart.Default.Settings.FormatErrorAction = SmartFormat.Core.Settings.ErrorAction.Ignore;
        }

        FormatCache _cache = null;

        public override async IAsyncEnumerable<object> Select(MessageContext context)
        {
            yield return await Task.Run<object>(() =>
            {
                return Smart.Default.FormatWithCache(ref _cache, Template, context);
            });
            yield break;
        }

        public TokenisedExpresion Tokenise(string tokenTemplate)
        {
            int argCounter = 1;
            Func<string> nextArgName = () => Smart.Format(tokenTemplate, argCounter++);

            var format = Smart.Default.Parser.ParseFormat(Template, null);
            var pieces = (from f in format.Items
                          let isPlaceholder = f is Placeholder
                          select new
                          {
                              isPlaceholder,
                              Raw = f.RawText,
                              Text = isPlaceholder
                                     ? nextArgName()
                                     : f.RawText
                          }).ToList();

            var placeHolders = pieces.Where(x => x.isPlaceholder);

            return new TokenisedExpresion
            {
                TokenisedExpression = String.Join("", pieces.Select(x => x.Text).ToArray()),
                ParameterTemplates = placeHolders.ToDictionary(x => x.Text, y => new DataSelector(Smart.Format("SF:{0}", y.Raw)))
            };
        }
    }
}
