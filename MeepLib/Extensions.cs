﻿using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;

using MeepLib.Messages;

namespace MeepLib
{
    /// <summary>
    /// Extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Convert line endings to Unix style
        /// </summary>
        /// <returns>The unix endings.</returns>
        /// <param name="text">Text.</param>
        public static string ToUnixEndings(this string text)
        {
            return text.Replace("\r\n", "\n");
        }

        /// <summary>
        /// Fetch the XmlRoot attribute of a type, if any
        /// </summary>
        /// <returns>The xml root.</returns>
        /// <param name="t">T.</param>
        public static XmlRootAttribute GetXmlRoot(this Type t)
        {
            return t.GetCustomAttributes(typeof(XmlRootAttribute), true).Cast<XmlRootAttribute>().FirstOrDefault();
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

        /// <summary>
        /// Add a module upstream
        /// </summary>
        /// <param name="module">Module.</param>
        /// <remarks>This is used when constructing pipelines in code, not
        /// when deserialising them.</remarks>
        public static void AddUpstream(this AMessageModule parent, AMessageModule module)
        {
            if (parent.Upstreams == null)
                parent.Upstreams = new List<AMessageModule>();

            parent.Upstreams.Add(module);
        }

        /// <summary>
        /// Find the first ancestor in the DerivedFrom chain that matches a type
        /// </summary>
        /// <returns>The ancestor.</returns>
        /// <param name="msg">Message.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        public static T FirstAncestor<T>(this Message msg) where T : Message
        {
            if (msg == null)
                return null;

            if (msg is T)
                return msg as T;

            if (msg.DerivedFrom == null)
                return null;

            if (msg.DerivedFrom is T)
                return msg.DerivedFrom as T;

            return FirstAncestor<T>(msg.DerivedFrom);
        }
    }
}
