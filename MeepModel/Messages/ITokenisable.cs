using System;
using System.Collections.Generic;

namespace MeepLib.Messages
{
    /// <summary>
    /// A message that can expose a word/feature inventory for Bayesian classification, Markov training, text search indexes, etc.
    /// </summary>
    public interface ITokenisable
    {
        /// <summary>
        /// Full list of all tokens
        /// </summary>
        /// <value>The tokens.</value>
        /// <remarks>These could be words, binned numbers, forums, etc.
        /// 
        /// <para>Do not do any reduction. Counting and stop words will be
        /// handled by the consumer.</para></remarks>
        IEnumerable<string> Tokens { get; }
    }
}
