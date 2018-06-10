using System;

using NCalc;

using MeepLib;
using MeepLib.MeepLang;
using MeepModel.Messages;
using System.Threading.Tasks;

namespace MeepLib.Filters
{
    /// <summary>
    /// 'Where' condition using compiled NCalc expressions
    /// </summary>
    [Macro(DefaultProperty = "Expr", Name = "Where", Position = MacroPosition.After)]
    public class Where : AMessageModule
    {
        /// <summary>
        /// Expression in [NCalc] format
        /// </summary>
        /// <value>The expr.</value>
        public string Expr
        {
            get {
                return _expr;
            }
            set {
                _expr = value;
                var expression = new Expression(_expr);
                Lambda = expression.ToLambda<Message, bool>();
            }
        }
        private string _expr;

        /// <summary>
        /// Compiled version of the expression
        /// </summary>
        /// <value>The lambda.</value>
        private Func<Message, bool> Lambda { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (Lambda(msg))
                        return msg;

                    return null;
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
