using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using NCalc;

namespace MeepLib.Algorithms
{
    /// <summary>
    /// Functions for calculating values derived from analysis of strings/text
    /// </summary>
    public static class StringMetrics
    {
        /// <summary>
        /// Pairs/trios of letters (bigrams/trigrams) that appear frequently in "keyboard smash" strings
        /// </summary>
        /// <remarks>These bigrams are common in keyboard smashes, but not in general English usage.</remarks>
        private static string[] smashgrams = new string[] {
            "fh","sk","jd","hf",
            "pq","rp","ql","wj","vb","pg",
            "dfh","hfd","sdf","adsr","sdt",
            "thg","tyt","fds","fdf","dtf","fjh",
            "gjh","yhg","srd","dgh","jkg",
            "dfg","jkdf","dfgh","ghjk","sfd",
            "wdw"
        };

        /// <summary>
        /// A measure of how much a string appears to be a "keyboard smash"
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        /// <remarks>A keyboard smash is when you drum on keys randomly, eg: "ajkhglawghjav"</remarks>
        public static double KeyboardSmashScore(this string s)
        {
            int smashgramCount = 1;
            s = s.ToLower();
            foreach (var smashgram in smashgrams)
                smashgramCount += Occurences(s, smashgram);

            return (double)smashgramCount / s.Length;
        }

        public static object KeyboardSmashScore(FunctionArgs args)
        {
            string input = Convert.ToString(args.Parameters[0].Evaluate());

            if (!(input is null))
                return KeyboardSmashScore(input);

            return 0.0;
        }

        /// <summary>
        /// Number of times a string appears in another string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int Occurences(this string s, string value)
        {
            int count = 0;
            int i = 0;
            while (i <= s.Length)
            {
                int pos = s.IndexOf(value, i);
                if (pos > 0)
                {
                    count++;
                    i = pos + 1;
                }
                else
                    i = s.Length + 1;
            }
            return count;
        }

        /// <summary>
        /// Returns bits of entropy represented in a given string, per 
        /// http://en.wikipedia.org/wiki/Entropy_(information_theory) 
        /// </summary>
        /// <remarks>From Jeff Atwood's self-answer at:
        /// https://codereview.stackexchange.com/questions/868/calculating-entropy-of-a-string</remarks>
        public static double ShannonEntropy(this string s)
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
            string input = Convert.ToString(args.Parameters[0].Evaluate());

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
            string input = Convert.ToString(args.Parameters[0].Evaluate());

            if (!(input is null))
                return input.Length;

            return 0;
        }

        /// <summary>
        /// Boolean Regex match
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object RegexMatch(FunctionArgs args)
        {
            string value = Convert.ToString(args.Parameters[0].Evaluate());
            string expression = Convert.ToString(args.Parameters[1].Evaluate());

            if (expression != null && value != null)
                return Regex.Match(value, expression).Success;

            return false;
        }

        /// <summary>
        /// Boolean test for a substring existing within a string value
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Contains(FunctionArgs args)
        {
            string input = Convert.ToString(args.Parameters[0].Evaluate());
            string value = Convert.ToString(args.Parameters[1].Evaluate());

            if (input != null && value != null)
                return input.Contains(value);

            return false;
        }

        /// <summary>
        /// Escape newlines and separators so a string can be used as the value in a CSV
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CSVEscaped(FunctionArgs args)
        {
            string input = Convert.ToString(args.Parameters[0].Evaluate());

            string separator = "\t";
            if (args.Parameters.Length > 1)
                separator = Convert.ToString(args.Parameters[1].Evaluate());

            string sepReplacement = "\\t";
            if (args.Parameters.Length > 2)
                sepReplacement = Convert.ToString(args.Parameters[2].Evaluate());


            return input.Replace("\n", "\\n").Replace("\r","\\r").Replace(separator, sepReplacement);
        }
    }
}
