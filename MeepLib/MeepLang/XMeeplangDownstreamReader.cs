using System;
using System.Linq;
using System.Xml;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.IO;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Expand Meeplang syntax sugar for downstream macros
    /// </summary>
    /// <remarks>A little trickier than upstream, because we're adding
    /// container elements behind the XML reader's current point.</remarks>
    public class XMeeplangDownstreamReader : XmlPassthroughReader
    {
        public XMeeplangDownstreamReader(XmlReader reader) : base(reader)
        {
        }

        private XMPReaderState state = XMPReaderState.relay;

        private bool deliveredEmptyElement = false;

        /// <summary>
        /// Injected container macros and their depth
        /// </summary>
        /// <remarks>This stack shadows the XML document's stacks of nested
        /// elements. We record the depth that we do an insertion so we know
        /// when to swap it back in to deliver the closing tag.</remarks>
        private Stack<(MacroSubstitution, int)> InjectedMacros = new Stack<(MacroSubstitution, int)>();

        public override bool Read()
        {
            bool _read = false;

            switch (state)
            {
                case XMPReaderState.relay:
                    _read = _Given.Read();
                    if (_read)
                        if (_Given.NodeType == XmlNodeType.Element && _Given.IsStartElement())
                        {
                            foreach (var m in ApplicableMacros(_Given))
                            {
                                state = XMPReaderState.substitute;
                                InjectedMacros.Push((m, _Given.Depth));
                                _Reader = m.Reader;
                                if (_Given.IsEmptyElement)
                                    deliveredEmptyElement = false;
                            }
                        }
                        else if (_Given.NodeType == XmlNodeType.EndElement && InjectedMacros.Count > 0)
                        {
                            // Check if item on top of stack has the same depth
                            // and swap it in to deliver the closing element.
                            var (macro, depth) = InjectedMacros.Peek();
                            if (depth == _Given.Depth)
                            {
                                var (substitute, subdepth) = InjectedMacros.Pop();
                                _Reader = substitute.Reader;
                                _read = _Reader.Read();
                                state = XMPReaderState.substituteEndtag;
                            }
                        }
                    break;
                case XMPReaderState.substitute:
                    if (_Given.IsEmptyElement && InjectedMacros.Count > 0 && deliveredEmptyElement)
                    {
                        var (macro, depth) = InjectedMacros.Peek();
                        if (depth == _Given.Depth)
                        {
                            state = XMPReaderState.substituteEndtag;
                            var (substitute, subdepth) = InjectedMacros.Pop();
                            _Reader = substitute.Reader;
                            _read = _Reader.Read();
                            break;
                        }
                    }
                    _Reader = _Given;
                    _read = true;
                    if (_Given.IsEmptyElement)
                        deliveredEmptyElement = true;
                    else
                        state = XMPReaderState.relay;
                    break;
                case XMPReaderState.substituteEndtag:
                    _Reader = _Given;
                    _read = _Given.Read();
                    state = XMPReaderState.relay;
                    break;
                default:
                    break;
            }

            return _read;
        }

        private IEnumerable<MacroSubstitution> ApplicableMacros(XmlReader reader)
        {
            List<MacroSubstitution> macros = new List<MacroSubstitution>();

            if (reader.IsStartElement())
            {
                string elementNS = reader.NamespaceURI;
                reader.MoveToFirstAttribute();
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    string nspace = String.IsNullOrWhiteSpace(reader.NamespaceURI) ? elementNS : reader.NamespaceURI;
                    var (mtype, macro) = MacroFinder.GetMacro(nspace, reader.LocalName);
                    if (macro != null && macro.Position == MacroPosition.Downstream)
                    {
                        XmlReader substitute = MacroToReader(macro, mtype, reader);
                        macros.Add(new MacroSubstitution
                        {
                            Macro = macro,
                            Attribute = reader.LocalName,
                            Reader = substitute
                        });
                    }

                    reader.MoveToNextAttribute();
                }
                reader.MoveToElement();
            }

            return macros;
        }

        private XmlReader MacroToReader(MacroAttribute macro, Type type, XmlReader current)
        {
            var xmlr = type.GetXmlRoot();
            string ns = xmlr?.Namespace ?? current.NamespaceURI;

            XmlDocument xdoc = new XmlDocument();
            XmlElement melement = xdoc.CreateElement(string.Empty, type.Name, ns);
            XmlAttribute prop = xdoc.CreateAttribute(string.Empty, macro.DefaultProperty, string.Empty);
            prop.Value = current.Value;
            melement.Attributes.Append(prop);
            melement.IsEmpty = false;
            xdoc.AppendChild(melement);

            XmlReader newreader = new XmlNodeReader(xdoc);
            newreader.Read();
            return newreader;
        }
    }

    internal enum XMPReaderState
    {
        relay,
        substitute,
        substituteEndtag
    }

    internal class MacroSubstitution
    {
        public MacroAttribute Macro;

        public string Attribute;

        public XmlReader Reader;
    }
}
