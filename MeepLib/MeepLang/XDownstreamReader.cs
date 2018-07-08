using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace MeepLib.MeepLang
{
    public class XDownstreamReader : XmlPassthroughReader
    {
        public XDownstreamReader(XmlReader reader) : base(reader)
        {
            CurrentState = new ReaderState
            {
                Reader = _Given,
                IsGiven = true
            };
        }

        private Stack<ReaderState> ReaderStack = new Stack<ReaderState>();

        /// <summary>
        /// Macros waiting to deliver their EndElement
        /// </summary>
        private Stack<ReaderState> MidStack = new Stack<ReaderState>();

        private ReaderState CurrentState;

        protected override XmlReader _Reader
        {
            get
            {
                return CurrentState.Reader;
            }
            set
            {
                return;
            }
        }

        public override bool Read()
        {
            bool givenRead = true;
            ReaderStack.Push(CurrentState);



            if (CurrentState.IsGiven)   // #### Master stream
            {
                if (CurrentState.Reader.NodeType == XmlNodeType.EndElement || CurrentState.Reader.IsEmptyElement)
                {
                    if (MidStack.Count > 0 && MidStack.Peek().Depth == CurrentState.Reader.Depth + 1)
                    {
                        // A macro needs to deliver its EndElement
                        ReaderStack.Push(MidStack.Pop());
                    }
                    else
                        givenRead = CurrentState.Reader.Read();
                }

                else
                {
                    givenRead = CurrentState.Reader.Read();
                    if (!givenRead)
                    {
                        ReaderStack.Pop();
                        if (ReaderStack.Count > 0 || MidStack.Count > 0)
                            throw new InvalidOperationException("Given reader concluded with macros still waiting to finish");
                        return false;
                    }

                    // Look for new macros
                    if (CurrentState.Reader.NodeType == XmlNodeType.Element)
                        foreach (var m in ApplicableMacros(CurrentState.Reader))
                            ReaderStack.Push(m);
                }
            }

            else                        // #### Macro stream
            {
                if (CurrentState.Reader.NodeType == XmlNodeType.Element)
                    // Macro has delivered its Element, so put it in MidStack to await its chance to deliver EndElement
                    MidStack.Push(ReaderStack.Pop());

                if (CurrentState.Reader.NodeType == XmlNodeType.EndElement)
                {
                    // Macro is all done, bye!
                    ReaderStack.Pop();
                    if (ReaderStack.Peek().IsGiven)
                        ReaderStack.Peek().Reader.Read();
                }
            }





            CurrentState = ReaderStack.Pop();
            if (!CurrentState.IsGiven)
                return _Reader.Read();
            return givenRead;
        }





        private IEnumerable<ReaderState> ApplicableMacros(XmlReader reader)
        {
            List<ReaderState> macros = new List<ReaderState>();

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
                        macros.Add(new ReaderState
                        {
                            Attribute = macro,
                            AttributeName = reader.LocalName,
                            Reader = substitute,
                            Depth = reader.Depth
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
            return newreader;
        }
    }
}
