using System;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Generate a Fibonacci sequence
    /// </summary>
    /// <remarks></remarks>
    public class Fibonacci : AMessageModule
    {
        /// <summary>
        /// Index in the sequence in {Smart.Format}
        /// </summary>
        /// <value>From.</value>
        /// <remarks>Only used if the input is not a NumericMessage with an integer number</remarks>
        public DataSelector From { get; set; }

        private int LastPosition = 0;

        public override async Task<Message> HandleMessage(Message msg)
        {
            long pos = 0;

            NumericMessage numeric = msg as NumericMessage;
            if (numeric != null)
                pos = (long)numeric.Number;
            else
                if (From != null)
            {
                MessageContext context = new MessageContext(msg, this);
                var fromMsg = await From.TrySelectLongAsync(context);
                if (!fromMsg.Parsed)
                    return null;
                pos = fromMsg.Value;
            }

            if (pos == 0)
                pos = LastPosition++;

            return new NumericMessage
            {
                DerivedFrom = msg,
                Number = Fib(pos)
            };
        }

        /// <summary>
        /// Return Nth Fibonacci number
        /// </summary>
        /// <returns></returns>
        /// <param name="n">N.</param>
        static ulong Fib(long n)
        {
            double sqrt5 = Math.Sqrt(5);
            double p1 = (1 + sqrt5) / 2;
            double p2 = -1 * (p1 - 1);


            double n1 = Math.Pow(p1, n + 1);
            double n2 = Math.Pow(p2, n + 1);
            return (ulong)((n1 - n2) / sqrt5);
        }
    }
}
