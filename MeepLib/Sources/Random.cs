using System;
using System.Threading.Tasks;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Generate random numbers
    /// </summary>
    public class Random : AMessageModule
    {
        public DataSelector Min { get; set; }

        public DataSelector Max { get; set; }

        // Have one RNG per instance, or y'all get the same number every time you call it within the same millisecond
        private System.Random _rand = new System.Random();

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            var parsedMin = await Min.TrySelectIntAsync(context);
            if (!parsedMin.Parsed)
            {
                logger.Warn("Couldn't parse Min value from {0} for {1}", Min.Value, this.Name);
                return null;
            }

            var parsedMax = await Max.TrySelectIntAsync(context);
            if (!parsedMax.Parsed)
            {
                logger.Warn("Couldn't parse Max value from {0} for {1}", Max.Value, this.Name);
                return null;
            }

                if (parsedMin.Value != 0 || parsedMax.Value != 0)
                    return new NumericMessage
                    {
                        DerivedFrom = msg,
                        Number = (Decimal)_rand.Next(parsedMin.Value, parsedMax.Value)
                    };
                else
                    return new NumericMessage
                    {
                        DerivedFrom = msg,
                        Number = (Decimal)_rand.NextDouble()
                    };
        }
    }
}
