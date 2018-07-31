using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

using MeepLib.Config;
using MeepLib.Messages;

namespace MeepLib
{
    /// <summary>
    /// Context of message and its environment for easy addressing in {Smart.Format} templates
    /// </summary>
    /// <remarks>EG: "{mdl.Name}: {msg.Value}" is a valid SmartFormat template.</remarks>
    public class MessageContext
    {
        public MessageContext(Message message, AMessageModule module)
        {
            msg = message;
            mdl = module;

            _cfgMutex.WaitOne();
            if (cfg == null)
                cfg = ANamable.InventoryByBase<ANamable>().ToDictionary(x => x.Name);
            _cfgMutex.ReleaseMutex();
        }

        /// <summary>
        /// The message being processed
        /// </summary>
        /// <value>The message.</value>
        public Message msg { get; set; }

        /// <summary>
        /// Module currently processing the message
        /// </summary>
        /// <value>The mdl.</value>
        public AMessageModule mdl { get; set; }

        /// <summary>
        /// Lookup on named modules
        /// </summary>
        /// <value></value>
        public static Dictionary<string,ANamable> cfg { get; set; }
        private static Mutex _cfgMutex = new Mutex(false);
    }
}
