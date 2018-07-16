using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    [DataContract]
    public class NumericMessage : Message
    {
        [DataMember, Index(IsUnique = false)]
        public Decimal Value { get; set; }
    }
}
