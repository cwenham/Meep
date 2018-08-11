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
    /// <summary>
    /// Bloom filter
    /// </summary>
    /// <remarks>Only allow messages that have/have never been seen by its sister
    /// module BloomSave. 
    /// 
    /// <para>Bloom filters are fast, can substitute for expensive "does exist"
    /// database queries, and use a miniscule amount of RAM, but the trade-off
    /// is that while they never have false negatives, they can have false 
    /// positives.</para>
    /// 
    /// <para>You can reduce Bloom's actions to one of two: 1) "My sister has
    /// probably seen this before", or 2) "My sister has definitely never seen 
    /// this before".</para>
    /// 
    /// <para>Bloom filters are good for:</para>
    /// 
    /// <list type="bullet">
    ///     <item>Randomly generating lots of unique names quickly without the
    /// expense of querying a database or API to see if it's already taken.</item>
    /// 
    ///     <item>Caches where you can afford to occasionally re-fetch something
    /// you already have, but cannot afford to skip anything you don't have. 
    /// (set Polarity="Pass")</item>
    /// </list>
    /// 
    /// </remarks>
    [Macro(Name = "BloomFilter", DefaultProperty = "Sieve", Position = MacroPosition.Upstream)]
    public class Bloom : AFilter
    {
        static Bloom()
        {
            RestoreSieves();
        }

        /// <summary>
        /// Capacity of bloom filter
        /// </summary>
        /// <remarks>10K hard-coded instead of configurable until there's a good
        /// reason to change either.</remarks>
        public const int CAPACITY = 10 * 1024;

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

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string sieve = Smart.Format(Sieve, context);
                string value = Smart.Format(From, context);

                if (!Sieves.ContainsKey(sieve))
                    // If the sieve doesn't exist yet, then technically we
                    // haven't seen it before and failed the test
                    return ThisFailedTheTest(msg);

                BloomFilter<string> filter = Sieves[sieve];
                if (filter.Contains(value))
                    return ThisPassedTheTest(msg);
                else
                    return ThisFailedTheTest(msg);
            });
        }

        internal static Dictionary<string, BloomFilter<string>> Sieves;
        private static Mutex _sieveMutex = new Mutex(false);

        private static void RestoreSieves()
        {
            _sieveMutex.WaitOne();
            if (Sieves == null)
            {
                string sieveDir = ResourceDirectory("BloomFilters");
                var collanders = from f in Directory.GetFiles(sieveDir, "*.flt")
                                 let name = Path.GetFileName(f).Replace(".flt", "")
                                 let bits = File.ReadAllBytes(f)
                                 let filter = new BloomFilter<string>(CAPACITY, null, new BitArray(bits))
                                 select new { name, filter };

                Sieves = collanders.ToDictionary(x => x.name, y => y.filter);
            }
            _sieveMutex.ReleaseMutex();
        }
    }
}
