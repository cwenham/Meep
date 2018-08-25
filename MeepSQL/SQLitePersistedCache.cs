using System;
using System.Linq;
using System.Collections.Concurrent;

using MeepLib;

namespace MeepSQL
{
    /// <summary>
    /// PersistedCache using SQLite as the persistence layer
    /// </summary>
    /// <remarks>Uses an in-memory thread-safe dictionary for L1 cache,
    /// writing to a SQLite database for L2.</remarks>
    public class SQLitePersistedCache : IPersistedCache
    {
        /// <summary>
        /// How long to keep values in L1 cache, in seconds
        /// </summary>
        private const int L1_TTL = 60 * 5;

        public SQLitePersistedCache(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; private set; }

        private ConcurrentDictionary<string, PCacheValue> L1 = new ConcurrentDictionary<string, PCacheValue>();

        public string StringGet(string key)
        {
            PCacheValue val = null;
            if (L1.TryGetValue(key, out val))
                if (val.Expires > DateTime.UtcNow)
                    return val.Value;
                else
                {
                    L1.TryRemove(key, out val);
                    // ToDo: Delete from L2
                }

            // ToDo: Look for it in L2
            return null;
        }

        public void StringSet(string key, string value, TimeSpan ttl)
        {
            PCacheValue val = new PCacheValue(value, ttl);
            L1.AddOrUpdate(key, val, (k, v) => val);

            // ToDo: Remove seldom used values from L1

            // ToDo: Save to L2
        }
    }

    public class PCacheValue
    {
        public PCacheValue(string val, TimeSpan ttl)
        {
            Value = val;
            Expires = DateTime.UtcNow + ttl;
            LastAccessed = DateTime.UtcNow;
            LastChanged = DateTime.UtcNow;
        }

        public string Value
        {
            get
            {
                LastAccessed = DateTime.UtcNow;
                return _value;
            }
            set
            {
                _value = value;
                LastChanged = DateTime.UtcNow;
            }
        }
        private string _value;

        public DateTime Expires { get; set; }

        public DateTime LastAccessed { get; set; }

        public DateTime LastSaved { get; set; }

        public DateTime LastChanged { get; set; }
    }
}
