using System;
using System.Linq;
using System.Xml.Serialization;
using System.Reactive.Linq;

using MeepLib.MeepLang;

using MM = MeepLib.Messages;

namespace MeepLib.Flow
{
    /// <summary>
    /// Gather messages into batches
    /// </summary>
    /// <remarks>Operates on the ferry model: the ferry sails either every
    /// MaxWait, or when it's full (MaxSize).
    /// 
    /// <para>Its main use is for modules that are batch-aware, such as database
    /// and file savers and APIs with bulk/batch options.</para>
    /// </remarks>
    [XmlRoot(ElementName = "Batch", Namespace = "http://meep.example.com/Meep/V1")]
    [Macro(Name = "Batch", DefaultProperty = "MaxSize", Position = MacroPosition.Downstream)]
    public class Batch : AMessageModule
    {
        [XmlAttribute]
        public int MaxSize { get; set; } = 10;

        [XmlAttribute(AttributeName = "MaxWait")]
        public string strMaxWait
        {
            get
            {
                return MaxWait.ToString();
            }
            set
            {
                MaxWait = TimeSpan.Parse(value);
            }
        }

        [XmlIgnore]
        public TimeSpan MaxWait { get; set; } = TimeSpan.FromMinutes(10);

        [XmlIgnore]
        public override IObservable<MM.Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = from b in UpstreamMessaging.Buffer(MaxWait, MaxSize)
                                select new MM.Batch
                                {
                                    Messages = b.ToList()
                                };

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
