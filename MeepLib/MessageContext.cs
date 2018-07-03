using System;
using System.Collections.Generic;

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
    }
}
