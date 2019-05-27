using System;
using System.Xml;

using MeepLib;
using MeepLib.Config;
using MeepLib.MeepLang;

namespace MeepSQL.Config
{
    /// <summary>
    /// User-defined SQL
    /// </summary>
    /// <remarks>For the user that wants to override automatic SQL generation.</remarks>
    [MeepNamespace(ASqlModule.PluginNamespace)]
    public class SQL: AConfig, IMeepDeserialisable
    {
        public DataSelector Content { get; set; }

        public void ReadXML(XmlReader reader)
        {
            this.Name = reader.GetAttribute("Name");

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
