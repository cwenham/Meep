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
    public class XMeeplangDownstreamReader : XmlReader
    {
        public XMeeplangDownstreamReader(XmlReader reader)
        {
            _Given = reader;
            _Reader = _Given;
        }

        /// <summary>
        /// Reader given in the constructor with the main document
        /// </summary>
        /// <value>The given.</value>
        private XmlReader _Given { get; set; }

        /// <summary>
        /// Reader we're reflecting at any moment
        /// </summary>
        /// <value>The reader.</value>
        /// <remarks>This is usually _Given, but will be switched with a
        /// temporary reader populated with a macro expansion as they are
        /// detected and handled.</remarks>
        private XmlReader _Reader { get; set; }

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
                reader.MoveToFirstAttribute();
                for (int i = 0; i < reader.AttributeCount; i++)
                {
                    var (mtype, macro) = MacroFinder.GetMacro(reader.LocalName);
                    if (macro != null && macro.Position == MacroPosition.Downstream)
                    {
                        XmlReader substitute = MacroToReader(macro, mtype, reader);
                        macros.Add(new MacroSubstitution {
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
            string doc = String.Format(@"<{0} {1}=""{2}""></{0}>",
                                                           type.Name,
                                                           macro.DefaultProperty,
                                                           current.Value);

            var newReader = XmlReader.Create(new StringReader(doc));
            newReader.Read();

            return newReader;
        }

        #region Pass-through
        public override int AttributeCount => _Reader.AttributeCount;

        public override string BaseURI => _Reader.BaseURI;

        public override int Depth => _Reader.Depth;

        public override bool EOF => _Reader.EOF;

        public override bool IsEmptyElement => _Reader.IsEmptyElement;

        public override string LocalName => _Reader.LocalName;

        public override string NamespaceURI => _Reader.NamespaceURI;

        public override XmlNameTable NameTable => _Reader.NameTable;

        public override XmlNodeType NodeType => _Reader.NodeType;

        public override string Prefix => _Reader.Prefix;

        public override ReadState ReadState => _Reader.ReadState;

        public override string Value => _Reader.Value;

        public override string GetAttribute(int i)
        {
            return _Reader.GetAttribute(i);
        }

        public override string GetAttribute(string name)
        {
            return _Reader.GetAttribute(name);
        }

        public override string GetAttribute(string name, string namespaceURI)
        {
            return _Reader.GetAttribute(name, namespaceURI);
        }

        public override string LookupNamespace(string prefix)
        {
            return _Reader.LookupNamespace(prefix);
        }

        public override bool MoveToAttribute(string name)
        {
            return _Reader.MoveToAttribute(name);
        }

        public override bool MoveToAttribute(string name, string ns)
        {
            return _Reader.MoveToAttribute(name, ns);
        }

        public override bool MoveToElement()
        {
            return _Reader.MoveToElement();
        }

        public override bool MoveToFirstAttribute()
        {
            return _Reader.MoveToFirstAttribute();
        }

        public override bool MoveToNextAttribute()
        {
            return _Reader.MoveToNextAttribute();
        }

        public override bool ReadAttributeValue()
        {
            return _Reader.ReadAttributeValue();
        }

        public override void ResolveEntity()
        {
            _Reader.ResolveEntity();
        }
        #endregion
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
