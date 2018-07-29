using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;

using NLog;
using Newtonsoft.Json;

using MeepLib.Messages;
using MeepLib.Config;

namespace MeepLib
{
    public abstract class AMessageModule : ANamable
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

        /// <summary>
        /// True if the module manages the cache itself
        /// </summary>
        /// <value></value>
        /// <remarks>For modules like Get, which will make a HEAD request to see
        /// if its cache is invalid, yet.</remarks>
        protected virtual bool SelfCaching { get; set; } = false;

        /// <summary>
        /// Time-To-Live for cached messages
        /// </summary>
        /// <value>The cache ttl.</value>
        /// <remarks>Set to zero to disable caching.</remarks>
        public TimeSpan CacheTTL { get; set; }

        /// <summary>
        /// Hard deadline for processing a message
        /// </summary>
        /// <value>The deadline.</value>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Soft deadline for processing before a warning is logged
        /// </summary>
        /// <value></value>
        /// <remarks>Set to &lt;= 0 to disable tardy logging.</remarks>
        public TimeSpan TardyAt { get; set; }

        public List<AMessageModule> Upstreams { get; set; } = new List<AMessageModule>();

        public List<AConfig> Config { get; set; } = new List<AConfig>();

        public virtual async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() => msg);
        }

        protected IObservable<Message> UpstreamMessaging
        {
            get
            {
                return Observable.Merge<Message>(Upstreams.Select(x => x.Pipeline));
            }
        }

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
            var result = GetCachedResult(msg);
            if (result != null)
                return result;

            Stopwatch watch = Stopwatch.StartNew();
            try
            {
                var task = HandleMessage(msg);
                task.Wait(Timeout);

                SaveToCache(task.Result, msg.GetKey());
                return task.Result;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{ex.GetType().Name} thrown when handling message: {ex.Message}");
            }
            finally
            {
                watch.Stop();
                if (TardyAt > TimeSpan.Zero)
                    logger.Warn("{0} took {1} to process a {2}", Name, watch.Elapsed, msg.GetType().Name);
            }

            return null;
        }

        /// <summary>
        /// Fetch cached result for an inbound message
        /// </summary>
        /// <returns>The cached result.</returns>
        /// <param name="msg">Message.</param>
        protected Message GetCachedResult(Message msg)
        {
            if (CacheTTL == TimeSpan.Zero)
                return null;

            string key = $"{this.Name}:{msg.GetKey()}";

            try
            {
                var cached = AHostProxy.Current.CachedStringGet(key);
                if (cached == null)
                    return null;

                Message result = JsonConvert.DeserializeObject<Message>(cached);
                result.DerivedFrom = msg;

                return result;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Save the result of processing a message to the cache
        /// </summary>
        /// <returns>The to cache.</returns>
        /// <param name="msg">Message.</param>
        /// <param name="key">Key.</param>
        protected Message SaveToCache(Message msg, string key)
        {
            if (CacheTTL == TimeSpan.Zero)
                return msg;

            try
            {
                AHostProxy.Current.CachedStringSet(key, JsonConvert.SerializeObject(msg), CacheTTL);
            }
            catch (Exception)
            {
                return msg;
            }

            return msg;
        }
    }
}
