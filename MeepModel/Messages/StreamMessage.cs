using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// A System.IO stream
    /// </summary>
    [DataContract]
    public class StreamMessage : Message
    {
        /// <summary>
        /// The stream
        /// </summary>
        /// <value>The stream.</value>
        [XmlIgnore, JsonIgnore, NotMapped]
        public Stream Stream { get; set; }
    }
}
