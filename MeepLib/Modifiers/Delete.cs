using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml.Serialization;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Config;
using MeepLib.Messages;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// HTTP Delete
    /// </summary>
    [XmlRoot(ElementName = "Delete", Namespace = "http://meep.example.com/Meep/V1")]
    public class Delete : AMessageModule
    {
        /// <summary>
        /// URL in {Smart.Format}
        /// </summary>
        /// <value>The URL.</value>
        [XmlAttribute]
        public string URL { get; set; }

        /// <summary>
        /// Headers to include in request
        /// </summary>
        /// <value>The headers.</value>
        [XmlArray(ElementName = "Headers")]
        public Header[] Headers { get; set; } = new Header[0];

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Delete, Smart.Format(URL, context));
            if (Headers != null)
                req.AddHeaders(context, Headers);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var result = await client.SendAsync(req);

                    return msg;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when deleting {1}: {2}", ex.GetType().Name, req.RequestUri.AbsoluteUri, ex.Message);
                return null;
            }

        }
    }
}
