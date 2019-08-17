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
        /// Class of message
        /// </summary>
        /// <value>The class.</value>
        public DataSelector Class { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            var tmsg = msg as ITokenisable;
            if (tmsg == null)
                return msg;

            MessageContext context = new MessageContext(msg, this);

            string dfClass = await Class.SelectStringAsync(context);

            ClassIndex cindex = Bayes.GetClass(dfClass);
            if (cindex == null)
            {
                cindex = new ClassIndex { Name = dfClass };
                Bayes.AddClass(cindex);
            }

            cindex.IncDocumentCount();

            foreach (string token in tmsg.Tokens)
                cindex.IncTokenCount(token);

            return msg;
        }
    }
}
