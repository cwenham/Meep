using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;
using MeepLib.Algorithms;

namespace MeepLib.Filters
{
    /// <summary>
    /// Filter messages that match a named class trained with BayesTrain
    /// </summary>
    /// <remarks>Uses portions from https://gist.github.com/hodzanassredin/4514157
    /// 
    /// <para>May change to tagging rather than filtering so we can use shared
    /// pipelines and still know what to do with the messages on the other side.</para>
    /// </remarks>
    public class Bayes : AMessageModule
    {
        /// <summary>
        /// Classes to test, in comma separated {Smart.Format}
        /// </summary>
        /// <value>Comma separated class names</value>
        /// <remarks>Leave empty or null to check all known classes.</remarks>
        public string Class { get; set; }

        /// <summary>
        /// Mimimum number of documents in training set to consider a class
        /// </summary>
        /// <value>The minimum training.</value>
        /// <remarks>To eliminate classes that have not yet been trained adequately
        /// and produce too many false positives. Defaults to 10.</remarks>
        public int MinTraining { get; set; } = 10;

        public override async Task<Message> HandleMessage(Message msg)
        {
            ITokenisable bmsg = msg as ITokenisable;
            if (bmsg == null)
                return msg;

            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string sfClass = Class != null ? Smart.Format(Class, context) : null;

                string[] peerNames = null;
                if (!String.IsNullOrWhiteSpace(sfClass))
                    peerNames = sfClass.Split(',');

                if (peerNames is null || peerNames.Length == 0)
                    peerNames = Classes.Keys.ToArray();

                var peers = (from k in peerNames
                             where Classes.ContainsKey(k)
                             select Classes[k]).ToArray();

                if (!(peers is null))
                    foreach (var peer in peers)
                    {
                        var msgTokens = bmsg.Tokens.ToList();

                        double likely = Prediction(msgTokens, peer, peers);

                        if (likely > 0.5)
                            Categories.AddToCategory(peer.Name, msg.ID);
                    }

                return msg;
            });
        }

        public static double Prediction(IEnumerable<string> tokens, string @class, IEnumerable<ClassIndex> peers)
        {
            if (Classes.ContainsKey(@class))
                return Prediction(tokens, Classes[@class], peers);
            return 0;
        }

        public static double Prediction(IEnumerable<string> tokens, ClassIndex index, IEnumerable<ClassIndex> peers)
        {
            double allDocsCount = (double)peers.Select(x => x.DocumentCount).Sum();
            double allUniqueTokens = (double)peers.Select(x => x.Tokens.Count()).Sum();
            double allWordsInClass = (double)index.Tokens.Values.Sum();

            var classProbability = Math.Log((double)index.DocumentCount / allDocsCount);
            var score = classProbability + tokens.Sum(x => Math.Log(((double)index.TokenCount(x) + 1) / (allUniqueTokens + allWordsInClass)));

            return Math.Pow(Math.E, score);
        }

        /// <summary>
        /// Adds a class to the static dictionary
        /// </summary>
        /// <param name="class">Class.</param>
        public static void AddClass(ClassIndex @class)
        {
            Classes.TryAdd(@class.Name, @class);
        }

        /// <summary>
        /// Fetch a class from the static dictionary
        /// </summary>
        /// <returns>The class.</returns>
        /// <param name="name">Name.</param>
        public static ClassIndex GetClass(string name)
        {
            if (Classes.TryGetValue(name, out var @class))
                return @class;
            else
                return null;
        }

        public static string[] ClassNames 
        {
            get 
            {
                return Classes.Keys.ToArray();
            }
        }

        /// <summary>
        /// Static dictionary of Bayesian classes, used systemwide
        /// </summary>
        private static ConcurrentDictionary<string, ClassIndex> Classes = new ConcurrentDictionary<string, ClassIndex>();
    }
}
