using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

using MeepLib.Sources;
using MeepLib.Messages;

namespace MeepLibTests.Sources
{
    public class EmitTests
    {
        [Fact]
        public void Selections()
        {
            var selections = Emit.GetSelections();
            Assert.NotEmpty(selections);

            Assert.NotEmpty(selections["US States"]);
            Assert.NotEqual("Alabama Shakes",selections["US States"][0]);
            Assert.Equal("Alabama", selections["US States"][0]);
        }

        [Fact]
        public async void Wraparound()
        {
            var Emitter = new Emit("Reindeer");

            // There are only 8 reindeer, so 11 should wrap-around to the third
            var msg1 = new Step
            {
                Number = 11
            };

            var vixen = await Emitter.HandleMessage(msg1);

            Assert.IsType<StringMessage>(vixen);
            Assert.Equal("Vixen", ((StringMessage)vixen).Value);
        }
    }
}
