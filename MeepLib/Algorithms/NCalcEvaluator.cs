using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Text;
using System.ComponentModel;
using System.Globalization;

using NCalc;
using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Algorithms
{
    /// <summary>
    /// Evaluate an NCalc expression
    /// </summary>
    [TypeConverter(typeof(NCalcEvaluatorConverter))]
    public class NCalcEvaluator
    {
        public NCalcEvaluator(string expression)
        {
            this.Expression = expression;

            parameterised = Expression.ToSmartParameterised("[arg{0}]");
            _expression = new Expression(parameterised[0]);
            _expression.EvaluateFunction += Expression_EvaluateFunction;

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
        /// Implicitly convert a string to an NCalcEvaluator
        /// </summary>
        /// <param name="value"></param>
        /// <remarks>Makes it easy to create an instance via assignment. This is not needed for the TypeConverter, but
        /// can be handy anyway.</remarks>
        public static implicit operator NCalcEvaluator(string value)
        {
            return new NCalcEvaluator(value);
        }

        public string Expression { get; private set; }

        public object Evaluate(MessageContext context)
        {
            try
            {
                expressionMutex.WaitOne();
                _expression.Parameters.Clear();
                for (int i = 1; i <= parameterised.Length - 1; i++)
                    _expression.Parameters.Add($"arg{i}", Smart.Format(parameterised[i], context).ToBestType());
                return _expression.Evaluate();
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                expressionMutex.ReleaseMutex();
            }
        }

        public bool EvaluateBool(MessageContext context)
        {
            return Convert.ToBoolean(Evaluate(context));
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

        /// <summary>
        /// Handler for NCalc's EvaluateFunction event
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <remarks>Looks for a function that suits the one named in the expression being evaluated.</remarks>
        void Expression_EvaluateFunction(string name, FunctionArgs args)
        {
            if (_functions.ContainsKey(name))
                args.Result = _functions[name](args);
        }

        private static Dictionary<string, Func<FunctionArgs, object>> _functions;

        private Expression _expression = null;
        private Mutex expressionMutex = new Mutex(false);
        private string[] parameterised = null;
    }

    /// <summary>
    /// Convert a string to an NCalcEvaluator
    /// </summary>
    /// <remarks>Makes it easy for our deserialiser to create instances from string attributes, so it's simple to
    /// use them as properties in an AMessageModule.</remarks>
    public class NCalcEvaluatorConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return false;
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string sValue = value as string;
            if (value is null)
                return null;

            return new NCalcEvaluator(sValue);
        }
    }
}
