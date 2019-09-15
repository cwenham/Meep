using System;
using System.Threading.Tasks;
using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Acknowledge a Message that is, or derives from an IAcknowledgableMessage
    /// </summary>
    public class Ack : AMessageModule
    {
        public async override Task<Message> HandleMessage(Message msg)
        {
            var ackable = msg.FirstByClass<IAcknowledgableMessage>();

            if (ackable is null)
                return null;

            if (ackable.HasAcknowledged)
                return null;

            await ackable.Acknowledge();

            return msg;
        }
    }
}
