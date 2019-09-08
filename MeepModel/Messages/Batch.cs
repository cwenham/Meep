using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeepLib.Messages
{
    /// <summary>
    /// Batch of messages
    /// </summary>
    [DataContract]
    public class Batch : Message
    {
        [DataMember, XmlElement, NotMapped]
        public IEnumerable<Message> Messages { get; set; }
    }
}
