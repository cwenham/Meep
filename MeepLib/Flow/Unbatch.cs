using System;
using System.Linq;
using System.Xml.Serialization;
using System.Reactive.Linq;

using MeepLib.MeepLang;

using MM = MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Break a Batch message out into individual messages
    /// </summary>
    /// <remarks>Often book-ended with the Batch module to pass on to modules
    /// that don't know how to work with batches.</remarks>
    [XmlRoot(ElementName = "Unbatch", Namespace = "http://meep.example.com/Meep/V1")]
    public class Unbatch : AMessageModule
    {
        [XmlIgnore]
        public override IObservable<MM.Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = from b in UpstreamMessaging
                                let batch = b as MM.Batch
                                where batch != null
                                from m in batch.Messages
                                select m;

                return _pipeline;
            }
            protected set
            {
                _pipeline = value;
            }
        }
        private IObservable<MM.Message> _pipeline;
    }
}
