using System;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Subscribe to ("tap") message stream from another, named module
    /// </summary>
    [Macro(Name = "Tap", DefaultProperty = "From", Position = MacroPosition.FirstUpstream)]
    public class Tap : AMessageModule
    {
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

        public AMessageModule Source { get; private set; }

        public override IObservable<Message> Pipeline { get => Source.Pipeline; protected set => base.Pipeline = value; }
    }
}
