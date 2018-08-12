using System;
using System.Threading.Tasks;
using System.Diagnostics;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Run a pipeline in a child process
    /// </summary>
    public class SubPipeline : AMessageModule
    {
        /// <summary>
        /// Pipeline definition file in {Smart.Format}
        /// </summary>
        /// <value>The file.</value>
        /// <remarks>Ignored if we're passed an IStreamMessage.</remarks>
        public string File { get; set; }

        /// <summary>
        /// True if we shouldn't bother tracking the process and passing on
        /// messages that make it to its gutter
        /// </summary>
        /// <value><c>true</c> if forget; otherwise, <c>false</c>.</value>
        public bool Forget { get; set; } = true;

        public override async Task<Message> HandleMessage(Message msg)
        {
            Process process = null;

            switch (msg)
            {
                case IStreamMessage sm:
                    process = AHostProxy.Current.SelfInvoke(await sm.Stream);
                    break;
                default:
                    MessageContext context = new MessageContext(msg, this);
                    string pipelineFile = Smart.Format(File, context);
                    process = AHostProxy.Current.SelfInvoke(pipelineFile);
                    break;
            }

            if (Forget)
                return msg;
            else
            {
                // ToDo: Consider, then design a way to attach to StdOut and
                // pass on messages that make it to its gutter. This would
                // require overriding Pipeline and supplying it from here
                // somehow.
            }

            return null;
        }
    }
}
