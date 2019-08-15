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
    }
}
