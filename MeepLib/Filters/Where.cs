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
        public bool Compile { get; set; } = false;

        /// <summary>
        /// Expression in [NCalc] format
        /// </summary>
        /// <value>The expr.</value>
        public string Expr { get; set; }

        /// <summary>
        /// Compiled version of the expression
        /// </summary>
        /// <value>The lambda.</value>
        private Func<Message, bool> _lambda { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_lambda == null && Compile)
                    {
                        var expression = new Expression(Expr);
                        _lambda = expression.ToLambda<Message, bool>();
                    }

                    if (_lambda != null)
                    {
                        if (_lambda(msg))
                            return ThisPassedTheTest(msg);
                    }
                    else
                    {
                        MessageContext context = new MessageContext(msg, this);
                        string sfExpr = Smart.Format(Expr, context);
                        var expression = new Expression(sfExpr);
                        expression.Parameters.Add("msg", msg);
                        expression.Parameters.Add("mdl", this);
                        if ((bool)expression.Evaluate())
                            return ThisPassedTheTest(msg);
                    }

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
