using System;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Config;
using MeepLib.Messages;

namespace MeepLib.Modifiers
{
    /// <summary>
    /// HTTP Delete
    /// </summary>
    public class Delete : AMessageModule
    {
        /// <summary>
        /// URL to send an HTTP DELETE to
        /// </summary>
        /// <value>The URL.</value>
        public DataSelector URL { get; set; }

        /// <summary>
        /// Headers to include in request
        /// </summary>
        /// <value>The headers.</value>
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

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string dsUrl = await URL.SelectStringAsync(context);
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Delete, dsUrl);
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
