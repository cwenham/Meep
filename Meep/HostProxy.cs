using System;
using System.Diagnostics;
using System.IO;
using System.Net;

using MeepLib;
using StackExchange.Redis;

namespace Meep
{
    /// <summary>
    /// Provide platform and host-specific functions and data
    /// </summary>
    public class HostProxy : AHostProxy
    {
        public HostProxy(IConnectionMultiplexer multiplexer)
        {
            Current = this;
            Multiplexer = multiplexer;
        }

        /// <summary>
        /// Redis multiplexer
        /// </summary>
        /// <value>The multiplexer.</value>
        private IConnectionMultiplexer Multiplexer { get; set; }

        public override string BaseDirectory
        {
            get
            {
                return Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            }
        }

        /// <summary>
        /// Network credentials from a secret store
        /// </summary>
        /// <value>The credentials.</value>
        public override ICredentials GetCredential(string host, int port)
        {
            throw new NotImplementedException();
        }

        public override string CachedStringGet(string key)
        {
            return Multiplexer?.GetDatabase().StringGet(key);
        }

        public override void CachedStringSet(string key, string value, TimeSpan ttl)
        {
            Multiplexer?.GetDatabase().StringSet(key, value, ttl);
        }

        /// <summary>
        /// Start a new process of ourselves with the provided pipeline file
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineFile">Pipeline file.</param>
        public override Process SelfInvoke(string pipelineFile)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start a new process of ourselves with the provided pipeline definition in a stream
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineDefinition">Pipeline definition.</param>
        public override Process SelfInvoke(Stream pipelineDefinition)
        {
            throw new NotImplementedException();
        }

        public override bool DevelMode
        {
            get
            {
                return File.Exists("devmode.switch");
            }
        }
    }
}
