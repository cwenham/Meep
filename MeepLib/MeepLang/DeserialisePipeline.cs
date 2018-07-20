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
using Mvp.Xml.XInclude;

using MeepLib.Config;
using MeepLib.Messages;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// A pipeline module that deserialises pipelines from XML
    /// </summary>
    /// <remarks>This makes it possible for Meep to self-host, so we can hard-code
    /// a pipeline that both loads a pipeline and also--when teamed up with 
    /// modules like Timer and FileChanges--monitors the source for changes.</remarks>
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
                    // Chain XmlReaders together to handle XIncludes, plugins 
                    // and macro expansion.
                    // A pipeline within a pipeline within a pipeline. Kiss my
                    // ass, Christopher Nolan.

                    XIncludingReader includingReader = new XIncludingReader(reader);
                    XDownstreamReader downstreamReader = new XDownstreamReader(includingReader);
                    XUpstreamReader upstreamReader = new XUpstreamReader(downstreamReader);

                    // The serialiser doesn't like XDownstreamReader right now,
                    // but if we fix the bug then remove the intermediate step
                    // below that reads into an XmlDocument and take it straight 
                    // from the reader instead, since this is more memory efficient.
                    XmlDocument doc = new XmlDocument();
                    doc.Load(upstreamReader);

                    // XmlSerialiser needs some hints to put AMessageModule
                    // derivatives into the "Upstreams" collection.
                    XmlAttributes attrs = AllXmlAttributes(typeof(AMessageModule));
                    XmlAttributeOverrides attrOverrides = new XmlAttributeOverrides();
                    attrOverrides.Add(typeof(AMessageModule), "Upstreams", attrs);

                    XmlSerializer serialiser = new XmlSerializer(typeof(Pipeline), attrOverrides);

                    serialiser.UnknownElement += Serialiser_UnknownElement;
                    //var tree = serialiser.Deserialize(meeplangReader) as AMessageModule;
                    var docReader = new XmlNodeReader(doc);
                    var tree = serialiser.Deserialize(docReader) as AMessageModule;

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

        /// <summary>
        /// Find all uses of XmlRoot on classes via reflection
        /// </summary>
        /// <returns>The xml attributes.</returns>
        /// <param name="baseClass">Base class.</param>
        public static XmlAttributes AllXmlAttributes(Type baseClass)
        {
            XmlAttributes attrs = new XmlAttributes();

            try
            {
                var modules = from a in AppDomain.CurrentDomain.GetAssemblies()
                              from t in TryGetTypes(a)
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

        /// <summary>
        /// Get types from an assembly, with silent exception handling
        /// </summary>
        /// <returns>The load types.</returns>
        /// <param name="assembly">Assembly.</param>
        /// <remarks>Prompted by exceptions thrown when loading Entity Framework
        /// assemblies, apparently because of some classes marked obsolete or
        /// removed between versions. This method may no longer be needed if
        /// this was a temporary or configuration problem.</remarks>
        private static Type[] TryGetTypes(Assembly assembly)
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
