using System;
using System.Net.Http;
using System.Xml.Serialization;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.Sources
{
    [XmlRoot(ElementName = "Post", Namespace = "http://meep.example.com/Meep/V1")]
    public class Post : AMessageModule
    {
        /// <summary>
        /// URL in {Smart.Format}
        /// </summary>
        /// <value>The URL.</value>
        [XmlAttribute]
        public string URL { get; set; }

        /// <summary>
        /// What to deliver in the POST
        /// </summary>
        /// <value>The payload.</value>
        /// <remarks>Defaults to message serialised to JSON.</remarks>
        [XmlElement]
        public string Payload { get; set; } = "{AsJSON}";

        protected virtual HttpMethod Method { get; set; } = HttpMethod.Post;

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            HttpRequestMessage req = new HttpRequestMessage(Method, Smart.Format(URL, context));

            if (msg is StreamMessage)
            {
                req.Content = new StreamContent(await ((StreamMessage)msg).Stream);
            }
            else
            {
                string payload = Smart.Format(Payload, context);
                req.Content = new StringContent(payload);
            }

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var result = await client.SendAsync(req);

                    return new WebMessage
                    {
                        DerivedFrom = msg,
                        Headers = result.Headers,
                        Stream = result.Content.ReadAsStreamAsync()
                    };
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when sending to {1}: {2}", ex.GetType().Name, req.RequestUri.AbsoluteUri, ex.Message);
                return null;
            }
        }
    }
}
