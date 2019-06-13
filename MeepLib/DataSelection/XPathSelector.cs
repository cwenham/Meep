using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;

using MeepLib;
using MeepLib.Messages;

namespace MeepLib.DataSelection
{
    [DataSelector(Prefix = "XP:", MessageType = typeof(XMLMessage))]
    public class XPathSelector : ADataSelector
    {
        public XPathSelector(string template) : base(template)
        {
            this.Template = template;
            xp_template = XPathExpression.Compile(template);
        }

        private XPathExpression xp_template;

        public override async IAsyncEnumerable<object> Select(MessageContext context)
        {
            XMLMessage xmsg = context.msg as XMLMessage;
            if (xmsg is null)
                throw new ArgumentException("Message must be an XMLMessage");

            XPathNodeIterator result = null;

            try
            {
                result = await Task.Run<XPathNodeIterator>(() =>
                {
                    XPathDocument doc = new XPathDocument(xmsg.GetReader());
                    return doc.CreateNavigator().Select(xp_template);
                });
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when evaluating XPath expression {1}: {2}", ex.GetType().Name, Template, ex.Message);
            }

            while (result != null && result.MoveNext())
                yield return result.Current.TypedValue;

            yield break;
        }
    }
}
