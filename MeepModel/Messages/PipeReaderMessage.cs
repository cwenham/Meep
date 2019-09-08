using System;
//using System.IO.Pipelines;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeepLib.Messages
{
    /// <summary>
    /// Messages drawn from a PipeReader (placeholder)
    /// </summary>
    /// <remarks>Work was started on this, but put on hold for a later version. This is now just a placeholder until
    /// work resumes or I decide to deprecate it.</remarks>
    [DataContract]
    public class PipeReaderMessage : Message
    {
        //[XmlIgnore, JsonIgnore, NotMapped]
        //public PipeReader Reader { get; set; }
    }
}
