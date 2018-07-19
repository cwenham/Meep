using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace MeepLib.Config
{
    [DataContract]
    public abstract class AConfig
    {
        [DataMember, XmlAttribute]
        public string Name { get; set; }
    }
}
