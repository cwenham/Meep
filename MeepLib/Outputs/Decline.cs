using System;
using System.Threading.Tasks;
using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Decline a Message that is, or derives from an IAcknowledgableMessage
    /// </summary>
    public class Decline : AMessageModule
    {
        public async override Task<Message> HandleMessage(Message msg)
        {
            var ackable = msg.FirstByClass<IAcknowledgableMessage>();

            if (ackable is null)
                return null;

            await ackable.Decline();

            return msg;
        }
    }
}
