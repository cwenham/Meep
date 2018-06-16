using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Diagnostics;
using System.Xml.Serialization;

using NLog;

using MeepModel.Messages;

namespace MeepLib
{
    [XmlRoot(ElementName = "Pipeline", Namespace = "http://meep.example.com/Meep/V1")]
    public abstract class AMessageModule
    {
        protected Logger logger
        {
            get
            {
                if (_logger == null)
                    _logger = LogManager.GetLogger(this.Name);

                return _logger;
            }
        }
        private Logger _logger;

        public AMessageModule()
        {
        }

        /// <summary>
        /// Name of the module
        /// </summary>
        /// <value>The name.</value>
        /// <remarks>This should be unique if it's to be addressed elsewhere in the pipeline,
        /// such as with the Tap module.</remarks>
        [XmlAttribute]
        public string Name
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_Name))
                    _Name = this.GetType().Name;

                return _Name;
            }

            set
            {
                // Maintain the directory of named modules.
                // This is used by modules that address other modules, such
                // as Tap.
                if (_Phonebook.ContainsKey(value))
                    _Phonebook.Remove(value);

                _Name = value;

                _Phonebook.Add(_Name, this);
            }
        }
        private string _Name;

        /// <summary>
        /// Time-To-Live for cached messages
        /// </summary>
        /// <value>The cache ttl.</value>
        /// <remarks>Set to zero to disable caching.</remarks>
        [XmlAttribute]
        public TimeSpan CacheTTL { get; set; }

        /// <summary>
        /// Hard deadline for processing a message
        /// </summary>
        /// <value>The deadline.</value>
        [XmlAttribute]
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Soft deadline for processing before a warning is logged
        /// </summary>
        /// <value></value>
        /// <remarks>Set to &lt;= 0 to disable tardy logging.</remarks>
        [XmlAttribute]
        public TimeSpan TardyAt { get; set; }

        public List<AMessageModule> Upstreams { get; set; }

        public virtual async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() => msg);
        }

        [XmlIgnore]
        protected IObservable<Message> UpstreamMessaging
        {
            get
            {
                return Observable.Merge<Message>(Upstreams.Select(x => x.Pipeline));
            }
        }

        [XmlIgnore]
        public virtual IObservable<Message> Pipeline
        {
            get
            {
                if (_Pipeline == null)
                    _Pipeline = from msg in UpstreamMessaging
                                let result = ShippingAndHandling(msg)
                                where result != null
                                select result;

                return _Pipeline;
            }
            protected set
            {
                _Pipeline = value;
            }
        }
        private IObservable<Message> _Pipeline;

        /// <summary>
        /// Handle a message, observing caching and timeouts as configured
        /// </summary>
        /// <returns></returns>
        /// <param name="msg">Message.</param>
        protected Message ShippingAndHandling(Message msg)
        {
            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                var task = HandleMessage(msg);
                task.Wait(Timeout);
                return task.Result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{ex.GetType().Name} thrown when handling message: {ex.Message}");
            }
            finally {
                watch.Stop();
                if (TardyAt > TimeSpan.Zero)
                    logger.Warn("{0} took {1} to process a {2}", Name, watch.Elapsed, msg.GetType().Name);
            }

            return null;
        }

        protected Dictionary<string, AMessageModule> _Phonebook { get; set; } = new Dictionary<string, AMessageModule>();
    }
}
