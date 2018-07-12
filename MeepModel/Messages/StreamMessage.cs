using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace MeepLib.Messages
{
    [DataContract]
    public class StreamMessage : Message
    {
        /// <summary>
        /// The stream
        /// </summary>
        /// <value>The stream.</value>
        [XmlIgnore]
        public Stream Stream { get; set; }
    }
}
