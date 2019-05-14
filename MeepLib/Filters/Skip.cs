using System;
using System.Linq;
using System.Reactive.Linq;

using MeepLib.MeepLang;

using MM = MeepLib.Messages;
using MeepLib.Algorithms;

namespace MeepLib.Flow
{
    /// <summary>
    /// Skip by a number of messages, or while a condition is met
    /// </summary>
    /// <remarks>This will only have an effect at the beginning of a pipeline's operation until the Skip By
    /// quantity/While condition has passed, and then it has no effect on the pipeline until restarted.
    /// </remarks>
    [Macro(Name = "Skip", DefaultProperty = "Arg", Position = MacroPosition.Downstream)]
    public class Skip : AMessageModule
    {
        /// <summary>
        /// Number of messages to skip
        /// </summary>
        public int By { get; set; }

        /// <summary>
        /// Boolean expression, messages while be skipped while this is true
        /// </summary>
        /// <remarks>Once it returns false, it stops testing and lets all subsequent messages go through. The advantage
        /// is that it runs much faster after that point than using &lt;Where&gt;, but would be unsuitable if you need
        /// the condition to be continuously tested all of the time.</remarks>
        public NCalcEvaluator While { get; set; }

        /// <summary>
        /// Arbitrary argument, will be tested to see if it's a number or an expression
        /// </summary>
        public string Arg
        {
            get
            {
                if (While != null)
                    return While.Expression;

                return Convert.ToString(By);
            }
            set
            {
                if (int.TryParse(value, out int _by))
                    By = _by;
                else
                    While = value;
            }
        }

        public override IObservable<MM.Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    if (By > 0)
                        _pipeline = from b in UpstreamMessaging.Skip(By)
                                    select b;
                    else if (While != null)
                        _pipeline = from b in UpstreamMessaging.SkipWhile(msg => WhileCondition(msg))
                                    select b;

                return _pipeline;
            }
            protected set
            {
                _pipeline = value;
            }
        }
        private IObservable<MM.Message> _pipeline;

        public bool WhileCondition(MM.Message msg)
        {
            try
            {
                return While.EvaluateBool(new MessageContext(msg, this));
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when evaluating Skip While expression: {1}", ex.GetType().Name, ex.Message);
                return false;
            }
        }
    }
}
