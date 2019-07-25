using System;
using System.Collections.Generic;

namespace MeepLib.DataSelection
{
    public class LiteralSelector : ADataSelector
    {
        public override async IAsyncEnumerable<object> Select(MessageContext context)
        {
            yield return Template;
            yield break;
        }
    }
}
