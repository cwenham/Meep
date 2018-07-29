using System;
using System.Xml;
using MeepLib.MeepLang;

namespace MeepLib.Config
{
    public class Text : AConfig, IMeepDeserialisable
    {
        public string Content { get; set; }

        public void ReadXML(XmlReader reader)
        {
            while (reader.NodeType != XmlNodeType.Text && reader.NodeType != XmlNodeType.EndElement)
            {
                if (!reader.Read())
                    return;
                    
                if (reader.NodeType == XmlNodeType.Text)
                    Content = reader.ReadContentAsString();
            }
        }
    }
}
