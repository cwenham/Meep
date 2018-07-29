using System;
using System.Xml;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Interface for classes that can deserialise their properties and content
    /// themselves. Similar to System.Xml.IXmlSerialisable
    /// </summary>
    public interface IMeepDeserialisable
    {
        void ReadXML(XmlReader reader);
    }
}
