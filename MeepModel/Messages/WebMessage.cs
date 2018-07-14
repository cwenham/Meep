using System;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Xml.Serialization;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    [DataContract]
    public class WebMessage : Message
    {
        /// <summary>
        /// URL the message came from/was delivered to
        /// </summary>
        /// <value>The URL.</value>
        [DataMember]
        public string URL { get; set; }

        /// <summary>
        /// Live context of the listener
        /// </summary>
        /// <value>The context.</value>
        /// <remarks>Only set if message originated from an HttpListener,
        /// this will hold the live context of the call so another module in
        /// the pipeline can respond, making the pipeline act as an HTTP server.
        /// </remarks>
        [XmlIgnore, JsonIgnore]
        public HttpListenerContext Context { get; set; }

        /// <summary>
        /// Headers of the response
        /// </summary>
        /// <value>The headers.</value>
        [DataMember]
        public HttpResponseHeaders Headers { get; set; }
    }
}
