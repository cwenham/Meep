﻿using System;
using System.Xml;
using System.IO;

namespace MeepLib.Messages
{
    public class XMLMessage : Message
    {
        /// <summary>
        /// Expose a reader for the underlying XML stream
        /// </summary>
        /// <value>The reader.</value>
        /// <remarks>By default, we'll assume Value is a string with XML, but
        /// subclasses could override this and get the stream from elsewhere.</remarks>
        public virtual XmlReader GetReader()
        {
            StringReader stringReader = new StringReader(Value.ToString());
            return XmlReader.Create(stringReader);
        }
    }
}
