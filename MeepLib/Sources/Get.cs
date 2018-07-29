using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;
using MeepLib.Config;
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

        /// <summary>
        /// Headers to include in request
        /// </summary>
        /// <value>The headers.</value>
        public Header[] Headers { get; set; }

        public string UserAgent { get; set; } = "Meep v1.0";

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, Smart.Format(URL, context));
            req.Headers.UserAgent.ParseAdd(UserAgent);
            if (Headers != null)
                req.AddHeaders(context, Headers);

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
                logger.Error(ex, "{0} thrown when getting {1}: {2}", ex.GetType().Name, req.RequestUri.AbsoluteUri, ex.Message);
                return null;
            }

        }
    }
}
