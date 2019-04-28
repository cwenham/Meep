using System;
//using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    [DataContract]
    public class PipeReaderMessage : Message
    {
        //[XmlIgnore, JsonIgnore, NotMapped]
        //public PipeReader Reader { get; set; }
    }
}
