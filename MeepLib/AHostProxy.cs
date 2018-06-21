using System;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace MeepLib
{
    /// <summary>
    /// Proxy for a Meep host running on a specific platform
    /// </summary>
    public interface IHostProxy
    {
        /// <summary>
        /// Source of network credentials (usernames, passwords) kept in a secret store
        /// </summary>
        ICredentials GetCredential(string host, int port);

        /// <summary>
        /// Start a new process of the host with the given pipeline definition file
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineFile">Pipeline file.</param>
        Process SelfInvoke(string pipelineFile);

        /// <summary>
        /// Start a new process of the host with the given pipeline definition stream
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineDefinition">Pipeline definition.</param>
        Process SelfInvoke(Stream pipelineDefinition);
    }
}
