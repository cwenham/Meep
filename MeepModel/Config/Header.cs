using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Config
{
    /// <summary>
    /// HTTP request header
    /// </summary>
    [DataContract]
    [XmlRoot(ElementName = "Header", Namespace = "http://meep.example.com/Meep/V1")]
    public class Header : AConfig
    {
        /// <summary>
        /// Header value in {Smart.Format}
        /// </summary>
        /// <value>The value.</value>
        [DataMember]
        [XmlAttribute]
        public string Value { get; set; }
    }
}
