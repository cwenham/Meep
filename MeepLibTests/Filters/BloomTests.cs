using System;

using Xunit;

using MeepLib.Filters;
using MeepLib.Messages;

namespace MeepLibTests.Filters
{
    public class BloomTests
    {
        /// <summary>
        /// Test restoration of sieves with the static constructor
        /// </summary>
        [Fact]
        public void StaticRestore()
        {
            Bloom bloom1 = new Bloom();

            Assert.NotNull(bloom1);
        }

        [Fact]
        public async void SaveAndFilter()
        {
            BloomTrain saver1 = new BloomTrain
            {
                Sieve = "UnitTest1"
            };

            Bloom bloom1 = new Bloom
            {
                Sieve = "UnitTest1"
            };

            StringMessage msg1 = new StringMessage(null, "Rhubarb");
            StringMessage msg2 = new StringMessage(null, "Custard");
            StringMessage msg3 = new StringMessage(null, "Marmite");
            StringMessage msg4 = new StringMessage(null, "Beeswax");

            Assert.Same(msg1, await bloom1.HandleMessage(msg1));
            Assert.Same(msg2, await bloom1.HandleMessage(msg2));
            Assert.NotSame(msg1, await bloom1.HandleMessage(msg2));

            await saver1.HandleMessage(msg1);
            Assert.Null(await bloom1.HandleMessage(msg1));
            Assert.NotNull(await bloom1.HandleMessage(msg2));

            await saver1.HandleMessage(msg2);
            Assert.Null(await bloom1.HandleMessage(msg2));
        }
    }
}
