using System;
using System.Net.Http.Headers;

namespace MeepLib.Messages
{
    public class WebMessage : Message
    {
        /// <summary>
        /// URL the message came from
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

        /// <summary>
        /// Headers of the response
        /// </summary>
        /// <value>The headers.</value>
        public HttpResponseHeaders Headers { get; set; }
    }
}
