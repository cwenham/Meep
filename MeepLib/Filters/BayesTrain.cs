using System;
using System.Threading.Tasks;

using SmartFormat;

using MeepLib.Messages;
using MeepLib.Algorithms;

namespace MeepLib.Filters
{
    /// <summary>
    /// Train a Bayesian classifier
    /// </summary>
    /// <remarks>Used with sister module Bayes</remarks>
    public class BayesTrain : AMessageModule
    {
        /// <summary>
        /// Class of message in {Smart.Format}, E.G.: "spam" or "ham"
        /// </summary>
        /// <value>The class.</value>
        public string Class { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            var tmsg = msg as ITokenisable;
            if (tmsg == null)
                return msg;

            return await Task.Run<Message>(() =>
            {
                MessageContext context = new MessageContext(msg, this);
                string bclass = Smart.Format(Class, context);

                if (!Bayes.Classes.ContainsKey(bclass))
                    Bayes.Classes.Add(bclass, new ClassIndex { Name = bclass });
                var cindex = Bayes.Classes[bclass];
                cindex.IncDocumentCount();

                foreach (string token in tmsg.Tokens)
                    cindex.IncTokenCount(token);

                return msg;
            });
        }
    }
}
