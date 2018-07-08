using System;
using System.Xml.Serialization;

namespace MeepLib
{
    [XmlRoot(ElementName = "Pipeline", Namespace = "http://meep.example.com/Meep/V1")]
    public class Pipeline : AMessageModule
    {
    }
}
