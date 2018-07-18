using System;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// An HTTP request or response
    /// </summary>
    [DataContract]
    public class WebMessage : StreamMessage
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
        [XmlIgnore, JsonIgnore, NotMapped]
        public HttpListenerContext Context { get; set; }

        /// <summary>
        /// Headers of the response
        /// </summary>
        /// <value>The headers.</value>
        [DataMember, NotMapped]
        public HttpResponseHeaders Headers { get; set; }
    }
}
