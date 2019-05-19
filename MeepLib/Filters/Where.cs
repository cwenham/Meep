using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using sle = System.Linq.Expressions;

using NCalc;
using SmartFormat;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using MeepLib.Algorithms;

namespace MeepLib.Filters
{
    /// <summary>
    /// 'Where' condition using compilable NCalc expressions
    /// </summary>
    [Macro(DefaultProperty = "Expr", Name = "Where", Position = MacroPosition.Upstream)]
    public class Where : AFilter
    {
        /// <summary>
        /// Boolean expression in {Smart.Format} and NCalc format
        /// </summary>
        /// <value>The expr.</value>
        /// <remarks>The expression should be supported by NCalc, but can contain
        /// Smart.Format placeholders because they'll be converted into NCalc
        /// parameters and passed in separately. E.G.:
        /// 
        /// <code>{msg.Number} > 256</code>
        /// 
        /// <para>Or:</para>
        /// 
        /// <code>{msg.FirstName} = 'Sally'</code>
        /// 
        /// </remarks>
        public NCalcEvaluator Expr { get; set; }

        /// <summary>
        /// Evaluate a Batch message's children (true), or just the container message (false--default)
        /// </summary>
        public bool Children { get; set; } = false;

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() =>
            {
                Batch batch = msg as Batch;
                if (Children && batch != null)
                    return HandleBatch(batch);
                else
                    return HandleSingle(msg);
            });
        }

        private Message HandleBatch(Batch batch)
        {
            foreach (var msg in batch.Messages)
                try
                {
                    MessageContext context = new MessageContext(msg, this);
                    bool result = Expr.EvaluateBool(context);

                    if (result)
                        return ThisPassedTheTest(batch);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{0} thrown on Where condition: {1}", ex.GetType().Name, ex.Message);
                    return null;
                }

            return ThisFailedTheTest(batch);
        }

        private Message HandleSingle(Message msg)
        {
            try
            {
                MessageContext context = new MessageContext(msg, this);
                bool result = Expr.EvaluateBool(context);

                if (result)
                    return ThisPassedTheTest(msg);
                else
                    return ThisFailedTheTest(msg);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown on Where condition: {1}", ex.GetType().Name, ex.Message);
                return null;
            }
        }
    }
}
