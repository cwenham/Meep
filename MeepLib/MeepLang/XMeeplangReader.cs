using System;
using System.Xml;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// Expand Meeplang syntax sugar for XML
    /// </summary>
    /// <remarks></remarks>
    public class XMeeplangReader : XmlReader
    {
        public XMeeplangReader(XmlReader reader)
        {
            _Reader = reader;
        }

        private XmlReader _Reader { get; set; }

        public override bool Read()
        {
            // ToDo: Implement this
            return _Reader.Read();
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
}
