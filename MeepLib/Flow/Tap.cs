﻿using System;
using System.Xml.Serialization;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Subscribe to ("tap") message stream from another, named module
    /// </summary>
    [XmlRoot(ElementName = "Tap", Namespace = "http://meep.example.com/Meep/V1")]
    [Macro(Name = "Tap", DefaultProperty = "From", Position = MacroPosition.FirstUpstream)]
    public class Tap : AMessageModule
    {
        [XmlAttribute]
        public string From
        {
            get => _From;
            set
            {
                _From = value;
                if (_Phonebook.ContainsKey(_From))
                    Source = _Phonebook[_From];

            }
        }
        public string _From;

        [XmlIgnore]
        public AMessageModule Source { get; private set; }

        [XmlIgnore]
        public override IObservable<Message> Pipeline { get => Source.Pipeline; protected set => base.Pipeline = value; }
    }
}
