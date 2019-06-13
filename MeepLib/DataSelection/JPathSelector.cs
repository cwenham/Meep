using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.DataSelection
{
    [DataSelector(Prefix = "JP:", MessageType = typeof(JTokenMessage))]
    public class JPathSelector : ADataSelector
    {
        public JPathSelector(string template) : base(template)
        {
            this.Template = template;
        }

        public override async IAsyncEnumerable<object> Select(MessageContext context)
        {
            JTokenMessage jmsg = context.msg as JTokenMessage;
            if (jmsg is null)
                throw new ArgumentException("Message must be a JTokenMessage");

            if (jmsg.Value != null)
            {
                IEnumerable<JToken> tokens = await Task.Run<IEnumerable<JToken>>(() =>
                {
                    return jmsg.Value.SelectTokens(Template);
                });

                foreach (var token in tokens)
                    yield return token.ToString();
            }

            yield break;
        }
    }
}
