﻿using System;
using System.Xml;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Base for On-The-Fly XML modifying readers
    /// </summary>
    /// <remarks>This implements all the required overrides. A modifying reader
    /// can then subclass this, override Read(), and swap _Reader with temporary
    /// substitutes to deliver the modifications.</remarks>
    public class XmlPassthroughReader : XmlReader
    {
        public XmlPassthroughReader(XmlReader reader)
        {
            _Given = reader;
            _Reader = _Given;
        }

        /// <summary>
        /// Reader given in the constructor with the main document
        /// </summary>
        /// <value>The given.</value>
        protected XmlReader _Given { get; set; }

        /// <summary>
        /// Reader we're reflecting at any moment
        /// </summary>
        /// <value>The reader.</value>
        /// <remarks>This is usually _Given, but may be switched with
        /// a substitute in the course of manipulating the XML on-the-fly.</remarks>
        protected virtual XmlReader _Reader { get; set; }

        /// <summary>
        /// Override me to inject custom behaviour
        /// </summary>
        /// <returns>The read.</returns>
        public override bool Read()
        {
            return _Reader.Read();
        }

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
    }

    internal class ReaderState
    {
        public bool IsGiven = false;

        public XmlReader Reader;

        public MacroAttribute Attribute;

        public string AttributeName;

        public int Depth;
    }
}
