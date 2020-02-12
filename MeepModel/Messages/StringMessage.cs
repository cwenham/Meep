using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Collections.Generic;

namespace MeepLib.Messages
{
    [DataContract]
    public class StringMessage : Message, IStringMessage, ITokenisable
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

        [NotMapped]
        public IEnumerable<string> Tokens
        {
            get
            {
                return Value.ExtractTokens();
            }
        }

        public override string ToString()
        {
            return Value;
        }
    }
}
