using System;
namespace MeepLib
{
    /// <summary>
    /// A cache that's persisted to disk or other permanent storage
    /// </summary>
    /// <remarks>Major implementations available that use Redis or SQLite
    /// for storage. Others can be made for Amazooglesoft cloud VMs with their
    /// own databases or Redis clones.
    /// 
    /// <para>The host app or plugins are expected to implement this interface
    /// so we can keep third party libraries out of MeepLib's main.</para></remarks>
    public interface IPersistedCache
    {
        /// <summary>
        /// Fetch a cached string value by its key
        /// </summary>
        /// <returns>The cached string.</returns>
        /// <param name="key">Key.</param>
        string StringGet(string key);

        /// <summary>
        /// Set a value in the cache, with a Time-to-Live (TTL)
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        /// <param name="ttl">Ttl.</param>
        void StringSet(string key, string value, TimeSpan ttl);
    }
}
