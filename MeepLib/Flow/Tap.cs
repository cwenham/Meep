using System;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Subscribe to ("tap") message stream from another, named module
    /// </summary>
    [Macro(Name = "Tap", DefaultProperty = "From", Position = MacroPosition.Child)]
    public class Tap : AMessageModule
    {
        /// <summary>
        /// Named module elsewhere in the pipeline
        /// </summary>
        /// <value></value>
        public string From { get; set; }

        public AMessageModule Source 
        {
            get {
                if (_Source == null)
                    _Source = ByName<AMessageModule>(From);

                if (_Source is null)
                    throw new ArgumentException($"Tap could not find: {From}", nameof(From));

                return _Source;
            }
        }
        private AMessageModule _Source;

        protected override IObservable<Message> GetMessagingSource()
        {
            return Source.Pipeline;
        }
    }
}
