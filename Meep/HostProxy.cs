using System;
using System.Diagnostics;
using System.IO;
using System.Net;

using MeepLib;
using MeepSQL;
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

        public override IPersistedCache Cache
        {
            get
            {
                if (_cache == null)
                {
                    if (Multiplexer != null)
                        return null; // ToDo: return RedisCache
                    else
                        _cache = new SQLitePersistedCache("Data Source=Meep.sqlite");
                }

                return _cache;
            }
        }
        private IPersistedCache _cache;

        /// <summary>
        /// Start a new process of ourselves with the provided pipeline file
        /// </summary>
        /// <returns>The invoke.</returns>
        /// <param name="pipelineFile">Pipeline file.</param>
        public override Process SelfInvoke(string pipelineFile)
        {
            using (var currentProc = Process.GetCurrentProcess())
            {
                var process = new Process();
                process.StartInfo.FileName = currentProc.MainModule.FileName;
                process.StartInfo.ArgumentList.Add("-bson");
                if (Multiplexer != null)
                    process.StartInfo.ArgumentList.Add($"-redis={Multiplexer.Configuration}");
                process.StartInfo.ArgumentList.Add(pipelineFile);
                process.Start();

                return process;
            }

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
