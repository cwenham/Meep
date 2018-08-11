using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// Batch of messages
    /// </summary>
    [DataContract]
    public class Batch : Message
    {
        [DataMember, XmlElement]
        public IEnumerable<Message> Messages { get; set; }
    }
}
