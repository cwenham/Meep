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

namespace MeepLib.Filters
{
    /// <summary>
    /// 'Where' condition using compilable NCalc expressions
    /// </summary>
    [Macro(DefaultProperty = "Expr", Name = "Where", Position = MacroPosition.Upstream)]
    public class Where : AFilter
    {
        public Where()
        {
            // Search for classes that implement compatible functions. If a plugin
            // has been loaded before this point, it'll discover functions exposed
            // from there. Must be a static class with functions that accept an
            // instance of FunctionArgs as their only parameter.

            if (_functions == null)
            {
                var staticClasses = from a in AppDomain.CurrentDomain.GetAssemblies()
                                    from t in a.GetTypes()
                                    where t.IsAbstract && t.IsSealed
                                    select t;

                var staticMethods = from t in staticClasses
                                    from f in t.GetMethods(BindingFlags.Static | BindingFlags.Public)
                                    select f;

                var matchingFuncs = from f in staticMethods
                                    let fParams = f.GetParameters()
                                    where fParams.Any()
                                    && fParams.Count() == 1
                                    && fParams.First().ParameterType == typeof(FunctionArgs)
                                    select f;

                _functions = matchingFuncs.ToDictionary(x => x.Name, ToFunc);
            }
        }

        /// <summary>
        /// Convert MethodInfo into a Func
        /// </summary>
        /// <returns>The func.</returns>
        /// <param name="method">MethodInfo</param>
        /// <remarks>Calling a delegate is significantly faster than using
        /// MethodInfo.Invoke, and typically only about 20% slower than a direct
        /// call.</remarks>
        private Func<FunctionArgs, object> ToFunc(MethodInfo method)
        {
            return (Func<FunctionArgs, object>)Delegate.CreateDelegate(typeof(Func<FunctionArgs, object>), method);
        }

        private static Dictionary<string, Func<FunctionArgs, object>> _functions;

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
        private Mutex expressionMutex = new Mutex(false);

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
                        expression.EvaluateFunction += Expression_EvaluateFunction;
                        // _lambda = expression.ToLambda<Message, Boolean>();
                    }

                    MessageContext context = new MessageContext(msg, this);
                    expressionMutex.WaitOne();
                    expression.Parameters.Clear();
                    for (int i = 1; i <= parameterised.Length - 1; i++)
                        expression.Parameters.Add($"arg{i}", Smart.Format(parameterised[i], context).ToBestType());
                    bool result = (bool)expression.Evaluate();
                    expressionMutex.ReleaseMutex();

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
            });
        }

        void Expression_EvaluateFunction(string name, FunctionArgs args)
        {
            if (_functions.ContainsKey(name))
                args.Result = _functions[name](args);
        }
    }
}
