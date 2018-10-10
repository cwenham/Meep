using System;
using System.Linq;
using System.Collections.Generic;
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

                string[] classes = null;
                if (!String.IsNullOrWhiteSpace(sfClass))
                    classes = sfClass.Split(',');

                if (classes is null || classes.Length == 0)
                    classes = Classes.Keys.ToArray();

                if (!(classes is null))
                    foreach (var c in classes)
                        if (Classes.ContainsKey(c) && Classes[c].DocumentCount >= MinTraining)
                        {
                            var bindex = Classes[c];

                            var msgTokens = bmsg.Tokens.ToList();

                            double likely = Prediction(msgTokens, bindex);

                            if (likely > 0.5)
                                Categories.AddToCategory(c, msg.ID);
                        }

                return msg;
            });
        }

        public static double Prediction(IEnumerable<string> tokens, string @class)
        {
            if (Classes.ContainsKey(@class))
                return Prediction(tokens, Classes[@class]);
            return 0;
        }

        public static double Prediction(IEnumerable<string> tokens, ClassIndex index)
        {
            var tokenPosteriori = from t in tokens
                                  let classOccurences = index.TokenCount(t)
                                  where classOccurences > 0
                                  select (double)classOccurences / index.DocumentCount;

            return tokenPosteriori.Aggregate(1.0, (current, item) => current * item);
        }

        internal static Dictionary<string, ClassIndex> Classes = new Dictionary<string, ClassIndex>();
    }
}
