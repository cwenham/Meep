using System;
using System.Net.Http;
using System.Xml.Serialization;

namespace MeepLib.Sources
{
    [XmlRoot(ElementName = "Put", Namespace = "http://meep.example.com/Meep/V1")]
    public class Put : Post
    {
        protected override HttpMethod Method { get; set; } = HttpMethod.Put;
    }
}
