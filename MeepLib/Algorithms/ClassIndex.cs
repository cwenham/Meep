using System;
using System.Linq;
using System.Collections.Concurrent;

namespace MeepLib.Algorithms
{
    /// <summary>
    /// Token index for a Bayes classifier
    /// </summary>
    public class ClassIndex
    {
        /// <summary>
        /// Last time this index was used
        /// </summary>
        /// <value>The last used.</value>
        /// <remarks>For expiring caches</remarks>
        public DateTime LastUsed { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of documents used to train this index
        /// </summary>
        /// <value>The document count.</value>
        public int DocumentCount = 0;

        /// <summary>
        /// Token counts
        /// </summary>
        /// <value>The tokens.</value>
        public ConcurrentDictionary<string, int> Tokens { get; protected set; } = new ConcurrentDictionary<string, int>();

        public void IncDocumentCount()
        {
            LastUsed = DateTime.UtcNow;
            System.Threading.Interlocked.Increment(ref DocumentCount);
        }

        /// <summary>
        /// Increase token count
        /// </summary>
        /// <param name="token">Token.</param>
        public void IncTokenCount(string token)
        {
            LastUsed = DateTime.UtcNow;
            Tokens.AddOrUpdate(token, 1, (t, val) => val + 1);
        }

        /// <summary>
        /// Return the count for a token in the training set
        /// </summary>
        /// <returns>The count.</returns>
        /// <param name="token">Token.</param>
        public int TokenCount(string token)
        {
            LastUsed = DateTime.UtcNow;

            if (Tokens.TryGetValue(token, out int value))
                return value;
            return 0;
        }

        public string ToSerialised()
        {
            // URLencode all tokens and delimit with "&". First 10 chars
            // are zero-padded document count

            var pairs = from k in Tokens.Keys
                        select String.Format("{0}={1}", System.Web.HttpUtility.UrlEncode(k), TokenCount(k));

            return String.Format("{0:0000000000}{1}", DocumentCount, String.Join("&", pairs));
        }

        public static ClassIndex Parse(string data)
        {
            string sDocCount = data.Substring(0, 10);
            if (!int.TryParse(sDocCount, out int docCount))
                throw new ArgumentException("Invalid format");

            var tokens = from p in data.Substring(10).Split('&')
                         let pieces = p.Split('=')
                         let token = System.Web.HttpUtility.UrlDecode(pieces[0])
                         let count = int.Parse(pieces[1])
                         select new { token, count };

            ClassIndex index = new ClassIndex();
            index.DocumentCount = docCount;
            foreach (var p in tokens)
                index.Tokens.AddOrUpdate(p.token, p.count, (t, val) => p.count);

            return index;
        }
    }
}
