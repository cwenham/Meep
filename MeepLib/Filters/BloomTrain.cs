using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

using SmartFormat;

using MeepLib.Algorithms;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Filters
{
    public class BloomTrain : AMessageModule
    {
        /// <summary>
        /// Name of the bit array used for the filter, in {Smart.Format}
        /// </summary>
        /// <value></value>
        /// <remarks>Defaults to "Bloom"</remarks>
        public string Sieve { get; set; } = "Bloom";

        /// <summary>
        /// Value to filter on, in {Smart.Format}
        /// </summary>
        /// <value></value>
        /// <remarks>Defaults to the message's .ToString() value.</remarks>
        public string From { get; set; } = "{msg.ToString}";

        /// <summary>
        /// How many updates before it persists the filter to disk
        /// </summary>
        /// <value></value>
        /// <remarks>The counter only increments if it's a true save and the
        /// value wasn't already in the filter.</remarks>
        public int PersistEvery { get; set; } = 10;

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string sieve = Smart.Format(Sieve, context);
                string value = Smart.Format(From, context);

                if (!Bloom.Sieves.ContainsKey(sieve))
                    Bloom.Sieves.Add(sieve, new BloomFilter<string>(Bloom.CAPACITY));

                BloomFilter<string> filter = Bloom.Sieves[sieve];

                if (!filter.Contains(value))
                {
                    filter.Add(value);
                    Interlocked.Increment(ref newSincePersist);
                }

                if (newSincePersist >= PersistEvery)
                {
                    // ToDo: Persist sieve to cache
                }

                return msg;
            });
        }

        private int newSincePersist = 0;
    }
}
