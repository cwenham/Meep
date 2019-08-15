using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Xunit;

using MeepLib;
using MeepLib.Messages;

namespace MeepLibTests
{
    public class SerialisationTests
    {
        [Fact]
        public void Serialise_Simple_Message()
        {
            string message_name = "Plain Vanilla";

            var msg = new Message
            {
                Name = message_name
            };

            MemoryStream stream = new MemoryStream();
            msg.SerialiseWithTypePrefixAsync(stream).GetAwaiter().GetResult();
            stream.Position = 0;

            Assert.InRange(stream.Length, 2, 1300);

            byte[] bmsg = new byte[stream.Length];
            stream.Read(bmsg, 0, bmsg.Length);
            string serialised = Encoding.UTF8.GetString(bmsg);

            Assert.Contains("MeepLib.Messages.Message", serialised);
            Assert.Contains(message_name, serialised);
        }

        [Fact]
        public void Deserialise_Simple_Message()
        {
            string message_name = "Milk Chocolate";
            string message_payload = "With Hazelnuts";

            // Don't make a plain message, so we can prove type is preserved
            var msg = new MeepLib.Messages.StringMessage
            {
                Name = message_name,
                Value = message_payload
            };

            // Serialise it
            MemoryStream stream = new MemoryStream();
            msg.SerialiseWithTypePrefixAsync(stream).GetAwaiter().GetResult();
            stream.Position = 0;

            byte[] bmsg = new byte[stream.Length];
            stream.Read(bmsg, 0, bmsg.Length);

            var deserialised = bmsg.DeserialiseOrBust();

            Assert.IsType(typeof(StringMessage), deserialised);
            Assert.Equal(message_name, deserialised.Name);
            Assert.Equal(message_payload, ((StringMessage)deserialised).Value);
        }
    }
}
