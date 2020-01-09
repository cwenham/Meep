using System;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace MeepLib
{
    /// <summary>
    /// Proxy for a Meep host running on a specific platform
    /// </summary>
    /// <remarks>This would be implemented by a program that hosts Meep pipelines, such as the Meep standalone project.
    /// </remarks>
    public abstract class AHostProxy
    {
        public AHostProxy()
        {
            Current = this;
        }

        public static AHostProxy Current { get; protected set; }

        /// <summary>
        /// Base/home directory Meep is running from
        /// </summary>
        /// <value>The base directory.</value>
        /// <remarks>Will be used to store plugins, pipeline definitions, and data files.</remarks>
        public abstract string BaseDirectory { get; }

        /// <summary>
        /// Start a new process of the host with the given pipeline definition file
        /// </summary>
        /// <returns></returns>
        /// <param name="pipelineFile">Pipeline file.</param>
        public abstract Process SelfInvoke(string pipelineFile);

        /// <summary>
        /// Start a new process of the host with the given pipeline definition stream
        /// </summary>
        /// <returns></returns>
        /// <param name="pipelineDefinition">Pipeline definition.</param>
        public abstract Process SelfInvoke(Stream pipelineDefinition);

        /// <summary>
        /// Return true if the user has activated development mode so unsigned plugins can be loaded
        /// </summary>
        /// <value><c>true</c> if devel mode; otherwise, <c>false</c>.</value>
        /// <remarks>Meep will load plugins without validation in development mode.</remarks>
        public virtual bool DevelMode { get; }
    }
}
