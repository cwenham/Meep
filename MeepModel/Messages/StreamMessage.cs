﻿using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// A System.IO stream
    /// </summary>
    [DataContract]
    public class StreamMessage : Message, IStreamMessage
    {
        /// <summary>
        /// The stream
        /// </summary>
        /// <value>The stream.</value>
        [XmlIgnore, JsonIgnore, NotMapped]
        public Task<Stream> Stream { get; set; }
    }
}
