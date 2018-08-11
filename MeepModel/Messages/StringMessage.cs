using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    [DataContract]
    public class StringMessage : Message
    {
        public StringMessage()
        { }

        public StringMessage(Message ancestor, string val)
        {
            this.DerivedFrom = ancestor;
            this.Value = val;
        }

        [DataMember]
        public string Value { get; set; }

        public override string ToString()
        {
            return Value;
        }
    }
}
