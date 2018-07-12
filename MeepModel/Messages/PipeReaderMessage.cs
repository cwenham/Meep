using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.IO.Pipelines;

namespace MeepLib.Messages
{
    [DataContract]
    public class PipeReaderMessage : Message
    {
        [XmlIgnore]
        public PipeReader Reader { get; set; }
    }
}
