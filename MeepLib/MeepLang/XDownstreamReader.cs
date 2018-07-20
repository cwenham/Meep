using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Replace downstream macros
    /// </summary>
    /// <remarks>Converts a MacroPosition.Downstream macro like "s:Save" here:
    /// 
    /// <code>
    ///     &lt;FetchSomething s:Save="Database:Table"/&gt;
    /// </code>
    /// 
    /// into this:
    /// 
    /// <code>
    ///     &lt;Upsert DBTable="Database:Table"&gt;
    ///         &lt;FetchSomething s:Save="Database:Table"/&gt;
    ///     &lt;/Upsert&gt;
    /// </code>
    /// 
    /// <para>(The original macro reference is left alone since it shouldn't
    /// affect the deserialiser.)</para>
    /// 
    /// <para>This is a little tricky because we need to read the input a node
    /// ahead of what we output, so we use a stack-based state machine.</para>
    /// 
    /// <para>KNOWN BUG: XmlSerialiser doesn't like our macro expansions,
    /// probably because we're Frankensteining document fragments together and
    /// something is confusing it. It fires an UnknownElement event when it
    /// encounters an expansion, but the LocalName and Namespace it found both
    /// appear to be correct. Sensitive to Parent/Sibling nodes, perhaps?</para>
    /// 
    /// <para>Workaround is to read it into an XmlDocument before passing it to 
    /// XmlSerialiser.Deserialise(), but this doesn't give us the memory
    /// efficiency we wanted from making it an XmlReader subclass in the first
    /// place.</para>
    /// 
    /// </remarks>
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

        /// <summary>
        /// Macro expansion logic
        /// </summary>
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
                        foreach (var m in ApplicableMacros(CurrentState.Reader, MacroPosition.Downstream))
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

    }
}
