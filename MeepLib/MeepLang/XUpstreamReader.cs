using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;

namespace MeepLib.MeepLang
{
    public class XUpstreamReader : XmlPassthroughReader
    {
        public XUpstreamReader(XmlReader reader) : base(reader)
        {
            CurrentState = new ReaderState
            {
                Reader = _Given,
                IsGiven = true
            };
        }

        /// <summary>
        /// XmlReader that we're currently reflecting
        /// </summary>
        private ReaderState CurrentState;

        public override bool Read()
        {
            // ToDo: Implement UpstreamReader
            return base.Read();
        }
    }
}
