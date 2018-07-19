using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Reflection;

using NLog;
using SmartFormat;

using MeepLib.Config;
using MeepLib.Messages;

namespace MeepLib.MeepLang
{
    [XmlRoot(ElementName = "DeserialisePipeline", Namespace = "http://meep.example.com/Meep/V1")]
    public class DeserialisePipeline : AMessageModule
    {
        public override async Task<Message> HandleMessage(Message msg)
        {
            XmlReader reader;
            if (msg is XMLMessage)
                reader = ((XMLMessage)msg).GetReader();
            else if (msg is StreamMessage)
                reader = XmlReader.Create(await ((StreamMessage)msg).Stream);
            else if (msg is StringMessage)
                reader = XmlReader.Create(new StringReader(((StringMessage)msg).Value));
            else
                return null;

            return await Task.Run<Message>(() =>
            {
                try
                {
                    XmlAttributes attrs = AllModuleXmlAttributes(typeof(AMessageModule));

                    XmlAttributeOverrides attrOverrides = new XmlAttributeOverrides();
                    attrOverrides.Add(typeof(AMessageModule), "Upstreams", attrs);

                    XmlSerializer serialiser = new XmlSerializer(typeof(Pipeline), attrOverrides);
                    XDownstreamReader meeplangReader = new XDownstreamReader(reader);

                    // Uncomment to inspect meeplangReader's output, just know it breaks the rest.
                    //XmlDocument doc = new XmlDocument();
                    //doc.Load(meeplangReader);

                    serialiser.UnknownElement += Serialiser_UnknownElement;
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

        void Serialiser_UnknownElement(object sender, XmlElementEventArgs e)
        {
            // ToDo: There's still a problem deserialising elements created from
            // macros by meeplangReader. Probably related to namespace prefixes
            // and the "xmlns" attrib. 
            Console.WriteLine(e.LineNumber);
            Console.WriteLine(e.Element.InnerXml);
        }


        public static XmlAttributes AllModuleXmlAttributes(Type baseClass)
        {
            XmlAttributes attrs = new XmlAttributes();

            try
            {
                var modules = from a in AppDomain.CurrentDomain.GetAssemblies()
                              from t in TryLoadTypes(a)
                              where t.IsSubclassOf(baseClass)
                              let r = t.GetXmlRoot()
                              select new { t, r };

                foreach (var t in modules)
                {
                    XmlElementAttribute attr = new XmlElementAttribute();
                    attr.ElementName = t.r != null ? t.r.ElementName : t.t.Name;
                    attr.Namespace = t.r.Namespace;
                    attr.Type = t.t;
                    attrs.XmlElements.Add(attr);
                }
            }
            catch (Exception ex)
            {
                Logger logger = LogManager.GetCurrentClassLogger();
                logger.Error(ex, "Error loading XmlAttributes");
            }

            return attrs;
        }

        public static Type[] TryLoadTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (Exception)
            {
                return new Type[] { };
            }
        }
    }
}
