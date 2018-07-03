using System;
using System.Xml;

namespace MeepLib
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        public static string ToUnixEndings(this string text)
        {
            return text.Replace("\r\n", "\n");
        }

        /// <summary>
        /// Copies the current node of reader to the writer
        /// </summary>
        /// <param name="reader">Reader.</param>
        /// <param name="writer">Writer.</param>
        /// <remarks>Does not copy the child elements like WriteNode() does.</remarks>
        public static void CopyCurrentNode(this XmlReader reader, XmlWriter writer)
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
                    writer.WriteAttributes(reader, true);
                    if (!String.IsNullOrWhiteSpace(reader.Value))
                        writer.WriteValue(reader.Value);
                    if (reader.IsEmptyElement)
                        writer.WriteEndElement();
                    break;
                case XmlNodeType.Text:
                    writer.WriteString(reader.Value);
                    break;
                case XmlNodeType.EndElement:
                    writer.WriteEndElement();
                    break;
                case XmlNodeType.Comment:
                    writer.WriteComment(reader.Value);
                    break;
                case XmlNodeType.Whitespace:
                    writer.WriteWhitespace(reader.Value);
                    break;
                default:
                    break;
            }
        }
    }
}
