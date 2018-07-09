using System;
using System.Linq;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Reflection;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.MeepLang
{
    [XmlRoot(ElementName = "DeserialisePipeline", Namespace = "http://meep.example.com/Meep/V1")]
    public class DeserialisePipeline : AMessageModule
    {
        public override async Task<Message> HandleMessage(Message msg)
        {
            var xmsg = msg as XMLMessage;
            if (xmsg == null)
                return null;

            return await Task.Run<Message>(() =>
            {
                try
                {
                    XmlAttributes attrs = AllModuleXmlAttributes();

                    XmlAttributeOverrides attrOverrides = new XmlAttributeOverrides();
                    attrOverrides.Add(typeof(AMessageModule), "Upstreams", attrs);

                    XmlSerializer serialiser = new XmlSerializer(typeof(Pipeline), attrOverrides);
                    XDownstreamReader meeplangReader = new XDownstreamReader(xmsg.GetReader());
                    var tree = serialiser.Deserialize(meeplangReader) as AMessageModule;

                    return new DeserialisedPipeline
                    {
                        DerivedFrom = msg,
                        Tree = tree
                    };
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{0} thrown when deserialising pipeline definition: {1}", ex.GetType().Name, ex.Message);
                    return null;
                }
            });
        }

        public static XmlAttributes AllModuleXmlAttributes()
        {
            XmlAttributes attrs = new XmlAttributes();

            var modules = from a in AppDomain.CurrentDomain.GetAssemblies()
                          from t in a.GetTypes()
                          where t.IsSubclassOf(typeof(AMessageModule))
                          let r = t.GetXmlRoot()
                          select new { t, r };

            foreach (var t in modules)
            {
                XmlElementAttribute attr = new XmlElementAttribute();
                attr.ElementName = t.r != null ? t.r.ElementName : t.t.Name;
                attr.Type = t.t;
                attrs.XmlElements.Add(attr);
            }

            return attrs;
        }
    }
}
