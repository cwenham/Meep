using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Expand upstream macros
    /// </summary>
    /// <remarks>Converts a MacroPosition.Upstream macro like Timer here:
    /// 
    /// <code>
    ///     &lt;CheckSomething Interval="00:15:00"/&gt;
    /// </code>
    /// 
    /// <para>into this:</para>
    /// 
    /// <code>
    ///     &lt;CheckSomething Interval="00:15:00"&gt;
    ///         &lt;Timer Interval="00:15:00"/&gt;
    ///     &lt;/CheckSomething&gt;
    /// </code>
    /// 
    /// <para>This is similar to XDownstreamReader and the other half of
    /// a divide-and-conquer for implementing MeepLang's macro expansion.</para>
    /// 
    /// </remarks>
    public class XUpstreamReader : XmlPassthroughReader
    {
        // The tricky part here is substituting non-empty elements for empty 
        // ones in the source stream and keeping track of where all of our 
        // substitutions are in a stack of nested elements.

        public XUpstreamReader(XmlReader reader) : base(reader)
        {
            CurrentState = new ReaderState
            {
                Reader = _Given,
                IsGiven = true
            };

            ReaderStack.Push(CurrentState);
        }

        /// <summary>
        /// Stack of XmlReaders waiting to be included in the combined document
        /// </summary>
        /// <remarks>The input reader (_Given) is pushed here along with readers
        /// for document fragments that contain our macro expansions.</remarks>
        private Stack<ReaderState> ReaderStack = new Stack<ReaderState>();

        /// <summary>
        /// Macros waiting to deliver their EndElement
        /// </summary>
        /// <remarks>Once the &lt;MacroElement&gt; is delivered, its XmlReader is
        /// saved here for its moment to deliver the closing &lt;/MacroElement&gt; 
        /// at the right place in the output.</remarks>
        private Stack<ReaderState> MidStack = new Stack<ReaderState>();

        /// <summary>
        /// XmlReader that we're currently reflecting
        /// </summary>
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
            CurrentState = ReaderStack.Pop();
            bool pushedCurrent = false;


            if (CurrentState.IsGiven)       // #### Master stream
            {
                if (CurrentState.Reader.NodeType == XmlNodeType.EndElement | CurrentState.Reader.IsEmptyElement)
                {
                    if (MidStack.Count > 0 && MidStack.Peek().Depth == CurrentState.Reader.Depth)
                        // A macro needs to deliver its EndElement
                        ReaderStack.Push(MidStack.Pop());
                }

                // Look for new macros
                if (CurrentState.Reader.NodeType == XmlNodeType.Element)
                {
                    var macros = ApplicableMacros(CurrentState.Reader, MacroPosition.Upstream).ToList();
                    if (macros.Any())
                    {
                        ReaderStack.Push(CurrentState);
                        pushedCurrent = true;
                        foreach (var m in macros)
                            ReaderStack.Push(m);
                    }
                }

            }

            else                            // #### Macro stream
            {
                if (CurrentState.Reader.NodeType == XmlNodeType.Element)
                {
                    // Has delivered its start element
                    MidStack.Push(CurrentState);
                    CurrentState = ReaderStack.Pop();
                }
                else if (CurrentState.Reader.NodeType == XmlNodeType.EndElement)
                {
                    // All done, bye
                    ReaderStack.Pop();
                }
            }


            if (!pushedCurrent)
                ReaderStack.Push(CurrentState);

            bool read = CurrentState.Reader.Read();
            return read;
        }

        /// <summary>
        /// Convert an IsEmptyElement to a non-empty element containing an expanded macro
        /// </summary>
        /// <returns>An XmlReader cued to return the parent element and its macro child</returns>
        /// <param name="current">Reader positioned at the empty element</param>
        /// <param name="macro">The ReaderState returned by ApplicableMacros()</param>
        /// <remarks>This handles empty elements that have upstream macros and
        /// we need to substitute a subtree for the original.</remarks>
        private XmlReader EmptyToParentAndMacro(XmlReader current, ReaderState macro)
        {
            // ToDo: Copy all of the attributes over, too
            XmlDocument xdoc = new XmlDocument();
            XmlElement parent = xdoc.CreateElement(current.Prefix, current.Name, current.NamespaceURI);
            parent.IsEmpty = false;

            XmlElement child = xdoc.CreateElement(macro.Reader.Prefix, macro.Reader.Name, macro.Reader.NamespaceURI);
            child.IsEmpty = true;
            parent.AppendChild(child);

            xdoc.AppendChild(parent);
            XmlReader newreader = new XmlNodeReader(xdoc);
            return newreader;
        }
    }
}
