using System;
using System.Xml;

using MeepLib;
using MeepLib.MeepLang;

namespace MeepSQL.Config
{
    /// <summary>
    /// User-defined SQL
    /// </summary>
    /// <remarks>For the user that wants to override automatic SQL generation.</remarks>
    public class SQL: ANamable, IMeepDeserialisable
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
