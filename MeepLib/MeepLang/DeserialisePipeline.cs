using System;
using System.Xml.Serialization;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.MeepLang
{
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
                    XmlSerializer serialiser = new XmlSerializer(typeof(Pipeline));
                    XMeeplangDownstreamReader meeplangReader = new XMeeplangDownstreamReader(xmsg.GetReader());
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
    }
}
