using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace MeepLib.Messages
{
    /// <summary>
    /// A parsed document or fragment of XML as an XContainer
    /// </summary>
    /// <remarks>XContainer was chosen because it's the first in the family
    /// that can contain other XObjects. If you need to store more primitive 
    /// XObjects like comments or attributes it's probably better to just 
    /// serialise them to a StringMessage because there's not going to
    /// be much benefit from the extra work of testing the type to see if we 
    /// can do things like enumerate nodes, query with XPath, etc.</remarks>
    public class XContainerMessage : Message
    {
        /// <summary>
        /// XContainer content
        /// </summary>
        public XContainer Value { get; set; }
    }
}
