using System;

using NCalc;
using SmartFormat;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Threading.Tasks;

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
        public string Expr { get; set; }

        /// <summary>
        /// Compiled version of the expression
        /// </summary>
        /// <value></value>
        /// <remarks>Unused for now. Need to develop a good way of getting
        /// {Smart.Formatted} parameters passed in properly.</remarks>
        private Func<Message, bool> _lambda { get; set; }

        private Expression expression = null;

        private string[] parameterised = null;

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (parameterised == null)
                    {
                        parameterised = Expr.ToSmartParameterised("[arg{0}]");
                        expression = new Expression(parameterised[0]);
                        // _lambda = expression.ToLambda<Message, Boolean>();
                    }

                    MessageContext context = new MessageContext(msg, this);
                    expression.Parameters.Clear();
                    for (int i = 1; i <= parameterised.Length - 1; i++)
                        expression.Parameters.Add($"arg{i}", Smart.Format(parameterised[i], context).ToBestType());

                    if ((bool)expression.Evaluate())
                        return ThisPassedTheTest(msg);
                    else
                        return ThisFailedTheTest(msg);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{0} thrown on Where condition: {1}", ex.GetType().Name, ex.Message);
                    return null;
                }
            });
        }
    }
}
