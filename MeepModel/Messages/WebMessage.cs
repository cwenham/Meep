using System;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using System.IO;

namespace MeepLib.Messages
{
    /// <summary>
    /// An HTTP request or response
    /// </summary>
    [DataContract]
    public class WebMessage : StreamMessage
    {
        public WebMessage(Task<Stream> stream)
        {
            _streamTask = stream;
        }

        private Task<Stream> _streamTask;

        public override async Task<Stream> GetStream()
        {
            return await _streamTask;
        }

        /// <summary>
        /// URL the message came from/was delivered to
        /// </summary>
        /// <value>The URL.</value>
        [DataMember]
        public string URL { get; set; }

        /// <summary>
        /// Headers of the response
        /// </summary>
        /// <value>The headers.</value>
        [DataMember, NotMapped]
        public HttpResponseHeaders Headers { get; set; }
    }
}
