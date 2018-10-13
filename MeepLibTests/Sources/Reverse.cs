using System;
using System.Linq;
using System.Threading.Tasks;

using MeepLib;
using MeepLib.Messages;

namespace MeepLibTests.Sources
{
    public class Reverse : AMessageModule
    {
        public override async Task<Message> HandleMessage(Message msg)
        {
            string reversedText = new string(msg.ToString().Reverse().ToArray());

            return new StringMessage
            {
                DerivedFrom = msg,
                Value = reversedText
            };
        }
    }
}
