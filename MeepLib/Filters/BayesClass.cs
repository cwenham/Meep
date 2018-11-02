using System;
using System.Linq;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Filters
{
    /// <summary>
    /// Emit trained ClassIndexes
    /// </summary>
    /// <remarks>These are the class indexes created and trained by BayesTrain.
    /// This module emits them on demand so they can be serialised to a store
    /// or sent to a sister pipeline.</remarks>
    public class BayesClass : AMessageModule
    {
        /// <summary>
        /// Name of the class to emit, or empty for all
        /// </summary>
        /// <value>From.</value>
        public string From { get; set; } = "";

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string sfFrom = Smart.Format(From, context);

                if (!String.IsNullOrWhiteSpace(sfFrom))
                    if (Bayes.Classes.ContainsKey(sfFrom))
                        return Bayes.Classes[sfFrom];
                    else
                        return null;

                var allClasses = from k in Bayes.Classes.Keys
                                 where !String.IsNullOrWhiteSpace(k)
                                    && !String.IsNullOrWhiteSpace(Bayes.Classes[k].Name)
                                 select Bayes.Classes[k];

                return new Batch
                {
                    DerivedFrom = msg,
                    Name = this.Name,
                    Messages = allClasses.ToList()
                };
            });
        }
    }
}
