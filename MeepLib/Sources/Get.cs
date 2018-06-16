using System;
using System.Net.Http;

using SmartFormat;

using MeepLib.MeepLang;
using MeepModel.Messages;
using System.Threading.Tasks;

namespace MeepLib.Sources
{
    /// <summary>
    /// HTTP GET a URL
    /// </summary>
    public class Get : AMessageModule
    {
        /// <summary>
        /// URL in {Smart.Format}
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, Smart.Format(URL, context));

            try
            {
                HttpClient client = new HttpClient();

                var result = await client.SendAsync(req);
                var returned = await result.Content.ReadAsStringAsync();

                return new WebMessage
                {
                    DerivedFrom = msg,
                    Headers = result.Headers,
                    Value = returned
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when getting {1}: {2}", ex.GetType().Name, req.RequestUri.AbsoluteUri, ex.Message);
                return null;
            }

        }
    }
}
