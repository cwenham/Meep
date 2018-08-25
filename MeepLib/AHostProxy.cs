﻿using System;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace MeepLib
{
    /// <summary>
    /// Proxy for a Meep host running on a specific platform
    /// </summary>
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
        /// <remarks>Will be used to store plugins, pipeline definitions, and
        /// data files.</remarks>
        public abstract string BaseDirectory { get; }

        /// <summary>
        /// Cache that's persisted to disk
        /// </summary>
        /// <value>The cache.</value>
        public abstract IPersistedCache Cache { get; }

        /// <summary>
        /// Start a new process of the host with the given pipeline definition file
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineFile">Pipeline file.</param>
        public abstract Process SelfInvoke(string pipelineFile);

        /// <summary>
        /// Start a new process of the host with the given pipeline definition stream
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineDefinition">Pipeline definition.</param>
        public abstract Process SelfInvoke(Stream pipelineDefinition);

        /// <summary>
        /// Return true if the user has activated development mode
        /// </summary>
        /// <value><c>true</c> if devel mode; otherwise, <c>false</c>.</value>
        /// <remarks>Meep will load plugins without validation in development
        /// mode.</remarks>
        public virtual bool DevelMode { get; }
    }
}
