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
            else if (msg is IStreamMessage)
                reader = XmlReader.Create(await ((IStreamMessage)msg).Stream);
            else if (msg is IStringMessage)
                reader = XmlReader.Create(new StringReader(((IStringMessage)msg).Value));
            else
                return null;

            return await Task.Run<Message>(() =>
            {
                try
                {
                    // Chain XmlReaders together to handle XIncludes, plugins 
                    // and macro expansion.
                    // A pipeline within a pipeline within a pipeline.

                    XIncludingReader includingReader = new XIncludingReader(reader);
                    XPluginReader pluginReader = new XPluginReader(includingReader);
                    XDownstreamReader downstreamReader = new XDownstreamReader(pluginReader);
                    XUpstreamReader upstreamReader = new XUpstreamReader(downstreamReader);

                    var deserialiser = new XMeeplangDeserialiser();
                    var tree = deserialiser.Deserialise(upstreamReader);

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
    }
}
