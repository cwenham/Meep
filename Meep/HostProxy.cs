using System;
using System.Diagnostics;
using System.IO;
using System.Net;

using MeepLib;

namespace Meep
{
    /// <summary>
    /// Provide platform and host-specific functions and data
    /// </summary>
    public class HostProxy : IHostProxy
    {
        public HostProxy()
        {
        }

        /// <summary>
        /// Network credentials from a secret store
        /// </summary>
        /// <value>The credentials.</value>
        public ICredentials GetCredential(string host, int port)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start a new process of ourselves with the provided pipeline file
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineFile">Pipeline file.</param>
        public Process SelfInvoke(string pipelineFile)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start a new process of ourselves with the provided pipeline definition in a stream
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineDefinition">Pipeline definition.</param>
        public Process SelfInvoke(Stream pipelineDefinition)
        {
            throw new NotImplementedException();
        }
    }
}
