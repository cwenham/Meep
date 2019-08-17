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
        public DataSelector URL { get; set; }

        /// <summary>
        /// Headers to include in request
        /// </summary>
        /// <value>The headers.</value>
        public Header[] Headers { get; set; }

        public DataSelector UserAgent { get; set; } = "Meep v1.0";

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string dsUrl = await URL.SelectStringAsync(context);
            string dsAgent = await UserAgent.SelectStringAsync(context);

            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, dsUrl);
            req.Headers.UserAgent.ParseAdd(dsAgent);
            if (Headers != null)
                req.AddHeaders(context, Headers);

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var result = await client.SendAsync(req);

                    return new WebMessage(result.Content.ReadAsStreamAsync())
                    {
                        DerivedFrom = msg,
                        Headers = result.Headers
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
