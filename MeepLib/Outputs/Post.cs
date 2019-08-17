using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;

using SmartFormat;

using MeepLib.Config;
using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.Outputs
{
    public class Post : AMessageModule
    {
        /// <summary>
        /// URL to HTTP POST to
        /// </summary>
        /// <value>The URL.</value>
        public DataSelector URL { get; set; }

        /// <summary>
        /// What to deliver in the POST
        /// </summary>
        /// <value>The payload.</value>
        /// <remarks>Defaults to message serialised to JSON.</remarks>
        public DataSelector Payload { get; set; } = "{AsJSON}";

        public IEnumerable<Header> Headers
        {
            get
            {
                if (_headers == null)
                    _headers = (from c in Config
                                where c is Header
                                select c as Header).ToArray();

                return _headers;
            }
        }
        private Header[] _headers;

        protected virtual HttpMethod Method { get; set; } = HttpMethod.Post;

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string dsUrl = await URL.SelectStringAsync(context);
            HttpRequestMessage req = new HttpRequestMessage(Method, dsUrl);
            if (Headers != null)
                req.AddHeaders(context, Headers);

            if (msg is IStreamMessage)
            {
                req.Content = new StreamContent(await ((IStreamMessage)msg).GetStream());
            }
            else
            {
                string payload = await Payload.SelectStringAsync(context);
                req.Content = new StringContent(payload);
            }

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
                logger.Error(ex, "{0} thrown when sending to {1}: {2}", ex.GetType().Name, req.RequestUri.AbsoluteUri, ex.Message);
                return null;
            }
        }
    }
}
