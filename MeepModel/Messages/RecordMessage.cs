using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MeepLib.Messages
{
    /// <summary>
    /// Hold a record, such as a DB row, as a String=Object dictionary
    /// </summary>
    [DataContract]
    public class RecordMessage : Message
    {
        [DataMember]
        public IDictionary<string, object> Record { get; set; }
    }
}
