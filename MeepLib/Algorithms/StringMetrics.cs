using System;
using System.Collections.Generic;

using NCalc;

namespace MeepLib.Algorithms
{
    /// <summary>
    /// Functions for calculating values derived from analysis of strings/text
    /// </summary>
    public static class StringMetrics
    {
        /// <summary>
        /// Returns bits of entropy represented in a given string, per 
        /// http://en.wikipedia.org/wiki/Entropy_(information_theory) 
        /// </summary>
        /// <remarks>From Jeff Atwood's self-answer at:
        /// https://codereview.stackexchange.com/questions/868/calculating-entropy-of-a-string</remarks>
        public static double ShannonEntropy(string s)
        {
            var map = new Dictionary<char, int>();
            foreach (char c in s)
            {
                if (!map.ContainsKey(c))
                    map.Add(c, 1);
                else
                    map[c] += 1;
            }

            double result = 0.0;
            int len = s.Length;
            foreach (var item in map)
            {
                var frequency = (double)item.Value / len;
                result -= frequency * (Math.Log(frequency) / Math.Log(2));
            }

            return result;
        }

        /// <summary>
        /// Version to be used in NCalc expressions
        /// </summary>
        /// <returns>The entropy.</returns>
        /// <param name="args">Arguments.</param>
        public static object ShannonEntropy(FunctionArgs args)
        {
            string input = (string)args.Parameters[0].Evaluate();

            if (!(input is null))
                return ShannonEntropy(input);

            return 0.0;
        }

        /// <summary>
        /// Length of string, for use in NCalc expressions
        /// </summary>
        /// <returns>The length.</returns>
        /// <param name="args">Arguments.</param>
        public static object Length(FunctionArgs args)
        {
            string input = (string)args.Parameters[0].Evaluate();

            if (!(input is null))
                return input.Length;

            return 0;
        }
    }
}
