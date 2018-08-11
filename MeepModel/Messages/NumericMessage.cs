using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    [DataContract]
    public class NumericMessage : Message, IStringMessage
    {
        [DataMember, Index(IsUnique = false)]
        public Decimal Number { get; set; }

        public string Value
        {
            get
            {
                return Number.ToString();
            }
        }
    }
}
