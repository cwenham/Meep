using System;
using System.Collections.Generic;
using System.Text;

using NCalc;

namespace MeepLib.Algorithms
{
    /// <summary>
    /// Static functions for NCalc expressions
    /// </summary>
    public static class DateTimeFuncs
    {
        /// <summary>
        /// How long since a given time
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object UtcAge(FunctionArgs args)
        {
            DateTime input = (DateTime)args.Parameters[0].Evaluate();
            if (input != null)
                return DateTime.UtcNow - input;

            return TimeSpan.Zero;
        }

        public static object InDays(FunctionArgs args)
        {
            TimeSpan input = (TimeSpan)args.Parameters[0].Evaluate();
            if (input != null)
                return input.TotalDays;

            return 0;
        }

        public static object InHours(FunctionArgs args)
        {
            TimeSpan input = (TimeSpan)args.Parameters[0].Evaluate();
            if (input != null)
                return input.TotalHours;

            return 0;
        }

        public static object InSeconds(FunctionArgs args)
        {
            TimeSpan input = (TimeSpan)args.Parameters[0].Evaluate();
            if (input != null)
                return input.TotalSeconds;

            return 0;
        }

        public static object InMilliseconds(FunctionArgs args)
        {
            TimeSpan input = (TimeSpan)args.Parameters[0].Evaluate();
            if (input != null)
                return input.TotalMilliseconds;

            return 0;
        }
    }
}
