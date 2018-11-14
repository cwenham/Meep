using System;
using System.Linq;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;
using MeepLib.Algorithms;

namespace MeepLib.Filters
{
    /// <summary>
    /// Emit or import trained ClassIndexes
    /// </summary>
    /// <remarks>These are the class indexes created and trained by BayesTrain.
    /// This module emits them on demand so they can be serialised to a store
    /// or sent to a sister pipeline.
    /// 
    /// <para>When given a RecordMessage, it looks to see if it can find
    /// ClassName, LastUsed, DocumentCount, and TokensString. If it finds them,
    /// it loads the data into a new ClassIndex in the global dictionary.</para></remarks>
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
                if (msg is Step)
                    return Export(msg);

                if (msg is RecordMessage)
                    return Import(msg);

                return null;
            });
        }

        public Message Export(Message msg)
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
        }

        public Message Import(Message msg)
        {
            // Elimination of invalid messages
            var record = msg as RecordMessage;
            if (record is null)
                return null;

            if (!record.Record.ContainsKey("ClassName"))
                return null;               

            if (!record.Record.ContainsKey("DocumentCount"))
                return null;

            if (!record.Record.ContainsKey("TokensString"))
                return null;

            //Okay, import it
            try
            {
                ClassIndex newClass = new ClassIndex
                {
                    Name = record.Record["ClassName"] as string,
                    DocumentCount = (int)record.Record["DocumentCount"],
                    TokensString = record.Record["TokensString"] as string
                };

                Bayes.Classes.Add(newClass.Name, newClass);

                return newClass;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"{ex.GetType().Name} thrown when importing ClassIndex: {ex.Message}");
            }

            return null;
        }
    }
}
