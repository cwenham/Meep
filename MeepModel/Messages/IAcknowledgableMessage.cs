using System;
using System.Threading.Tasks;

namespace MeepLib.Messages
{
    /// <summary>
    /// Interface for Messages that have the concept of acknowledgement, such as Queue messages
    /// </summary>
    public interface IAcknowledgableMessage
    {
        /// <summary>
        /// Acknowledge receipt of the message
        /// </summary>
        /// <returns></returns>
        ValueTask Acknowledge();

        /// <summary>
        /// Decline the message
        /// </summary>
        /// <returns></returns>
        ValueTask Decline();

        /// <summary>
        /// Automatically acknowledge a message once it reaches the gutter
        /// </summary>
        bool AutoAck { get; set; }

        /// <summary>
        /// Set to true when the message has been acknowledged, so it isn't acknowledged twice
        /// </summary>
        bool HasAcknowledged { get; protected set; }
    }
}
