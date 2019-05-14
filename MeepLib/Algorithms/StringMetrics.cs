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
        /// Pairs/trios of letters (bigrams/trigrams) that appear frequently in "keyboard smash" strings
        /// </summary>
        /// <remarks>These bigrams are common in keyboard smashes, but not in general English usage.</remarks>
        private static string[] smashgrams = new string[] {
            "dj","fh","sk","jd","hf",
            "pq","rp","ql","wj","vb","pg",
            "jsm","dfh","hfd","sdf","adsr","sdt",
            "thg","tyt","fds","dtf","fjh","gjh",
            "yhg"
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
    }
}
