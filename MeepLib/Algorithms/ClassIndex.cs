using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

using Newtonsoft.Json;

using MeepLib.Messages;

namespace MeepLib.Algorithms
{
    /// <summary>
    /// Token index for a Bayes classifier
    /// </summary>
    [DataContract]
    public class ClassIndex : Message
    {
        [MaxLength(64), Key]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// Last time this index was used
        /// </summary>
        /// <value>The last used.</value>
        /// <remarks>For expiring caches</remarks>
        [Index(IsUnique = false)]
        public DateTime LastUsed { get; private set; } = DateTime.UtcNow;

        /// <summary>
        /// Number of documents used to train this index
        /// </summary>
        /// <value>The document count.</value>
        [DataMember, Index(IsUnique = false)]
        public int DocumentCount 
        {
            get
            {
                return _documentCount;
            }
            set
            {
                _documentCount = value;
            }
        }
        private int _documentCount = 0;

        /// <summary>
        /// Token counts
        /// </summary>
        /// <value>The tokens.</value>
        [XmlIgnore, JsonIgnore, NotMapped]
        public ConcurrentDictionary<string, int> Tokens { get; protected set; } = new ConcurrentDictionary<string, int>();

        /// <summary>
        /// Serialise/Deserialise Tokens to URL encoded string
        /// </summary>
        /// <value>The tokens string.</value>
        [DataMember, XmlElement(ElementName = "Tokens"), JsonProperty(PropertyName = "Tokens"), MaxLength(1024 * 1024), Column("Tokens")]
        public string TokensString
        {
            get 
            {
                var pairs = from k in Tokens.Keys
                            select String.Format("{0}={1}", System.Web.HttpUtility.UrlEncode(k), TokenCount(k));

                return String.Join("&", pairs);
            }
            set {
                var tokens = from p in value.Split('&')
                             let pieces = p.Split('=')
                             let token = System.Web.HttpUtility.UrlDecode(pieces[0])
                             let count = int.Parse(pieces[1])
                             select new { token, count };

                foreach (var p in tokens)
                    Tokens.AddOrUpdate(p.token, p.count, (t, val) => p.count);
            }
        }

        public void IncDocumentCount()
        {
            LastUsed = DateTime.UtcNow;
            System.Threading.Interlocked.Increment(ref _documentCount);
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

            return String.Format("{0:0000000000}{1}", DocumentCount, TokensString);
        }

        /// <summary>
        /// Parse a ClassIndex serialised with ToSerialised()
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="data">Data.</param>
        public static ClassIndex Parse(string data)
        {
            string sDocCount = data.Substring(0, 10);
            if (!int.TryParse(sDocCount, out int docCount))
                throw new ArgumentException("Invalid format");

            string encodedTokens = data.Substring(10);

            ClassIndex index = new ClassIndex();
            index.DocumentCount = docCount;
            index.TokensString = encodedTokens;

            return index;
        }
    }
}
