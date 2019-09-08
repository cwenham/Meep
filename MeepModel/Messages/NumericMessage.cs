using System;
using System.Runtime.Serialization;

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
