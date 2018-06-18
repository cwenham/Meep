using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

using MeepModel.Messages;

namespace MeepLib.Modify
{
    /// <summary>
    /// Randomly recombine contents of two XML messages
    /// </summary>
    /// <remarks>Foundation for basic genetic algorithms.
    /// 
    /// <para>This waits for at least two genome-bearing messages to arrive from
    /// upstream, then recombines them and passes Offspring number of messages 
    /// downstream.</para>
    /// 
    /// <para>To implement a geneting algorithm pipeline, implement your scoring
    /// function as a filter and put two upstream, then use the Tap module to loop
    /// the output of Recombine back to the start. Add a mutation step to taste.</para>
    /// 
    /// <para>For best results, genomes should be normalised before recombination
    /// so their structures match.</para>
    /// </remarks>
    public class Recombine : AMessageModule
    {
        /// <summary>
        /// Namespace of elements to recombine
        /// </summary>
        /// <value>The namespace.</value>
        /// <remarks>Only elements of this namespace will be recombined, while
        /// elements of other namespaces will be passed through from the chosen
        /// parent.
        /// 
        /// <para>E.G.: "gene:" is the active namespace below:</para>
        /// 
        /// <code>&lt;gene:wing mode="flightless"&gt;
        ///     &lt;foo:plumage color="yellow"/&gt;
        ///     &lt;gene:dimensions length="long"/&gt;
        /// &lt;/gene:wing&gt;
        /// 
        /// &lt;gene:wing mode="flight"&gt;
        ///     &lt;foo:plumage color="purple"/&gt;
        ///     &lt;gene:dimensions length="short"/&gt;
        /// &lt;/gene:wing&gt;
        /// </code>
        /// 
        /// <para>"wing" and "dimensions" would be recombined randomly, but the
        /// plumage color would be passed through from the parent. That means 
        /// yellow wings would always be flightless, purple wings always flight 
        /// capable, but either could be long or short.</para>
        /// 
        /// <para>Precede with a $ to treat it as a regex.</para>
        /// </remarks>
        public string Namespace
        {
            get
            {
                return rNamespace?.ToString();
            }
            set
            {
                if (value[0] == '$')
                    rNamespace = new Regex(value.Substring(1), RegexOptions.Compiled);
                else
                    rNamespace = new Regex(Regex.Escape(value), RegexOptions.Compiled);
            }
        }
        private Regex rNamespace = new Regex(".*", RegexOptions.Compiled);

        /// <summary>
        /// How many offspring to produce in each generation
        /// </summary>
        /// <value>Number of offspring.</value>
        /// <remarks>Defaults to 5.</remarks>
        public int Offspring { get; set; } = 5;

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (Upstreams.Count > 2)
                    logger.Warn("More than two upstream modules for Recombine:{0}", Name);

                if (Upstreams.Count < 2)
                {
                    logger.Fatal("Not enough upstream modules for Recombine:{0}. Needs 2", Name);
                    throw new InvalidOperationException("Not enough upstream sources for Recombine module");
                }

                // Use Zip instead of Merge because we want only two parents
                var parents = Upstreams.Take(2);
                var pairings = parents.First().Pipeline.Zip<Message, Message, (Message, Message)>(parents.Last().Pipeline, (mum, dad) => (mum, dad));
                return from p in pairings
                       where p.Item1 is XMLMessage && p.Item2 is XMLMessage
                       let offspring = GenerateOffspring(p, Offspring)
                       where offspring != null
                       from o in offspring
                       select o;

            }
            protected set => base.Pipeline = value;
        }

        public IEnumerable<Message> GenerateOffspring((Message, Message) parents, int children)
        {
            if (!(parents.Item1 is XMLMessage && parents.Item2 is XMLMessage))
                return null;
                
            return from i in Enumerable.Range(1, children).AsParallel()
                   select ReCombine((XMLMessage)parents.Item1, (XMLMessage)parents.Item2);
        }

        public XMLMessage ReCombine(XMLMessage mum, XMLMessage dad)
        {
            int coin = Rand.Next(0, 1);
            XMLMessage p1 = coin == 0 ? mum : dad;
            XMLMessage p2 = coin == 1 ? dad : mum;

            bool moreLeft = true;
            StringBuilder builder = new StringBuilder();
            using (StringWriter sWriter = new StringWriter(builder))
            using (XmlWriter writer = XmlWriter.Create(sWriter))
            using (XmlReader r1 = p1.GetReader())
            using (XmlReader r2 = p2.GetReader())
            {
                XmlReader current = r1;

                moreLeft = r1.Read() && r2.Read();
                while (moreLeft)
                {
                    if (rNamespace.Match(current.NamespaceURI).Success)
                    {
                        coin = Rand.Next(0, 1);
                        current = coin == 0 ? r1 : r2;
                    }

                    writer.WriteNode(current, true);
                    if (current == r1)
                        moreLeft = r2.Read();
                    else
                        moreLeft = r1.Read();
                }
            }

            return new XMLMessage
            {
                DerivedFrom = p1,
                Value = builder.ToString()
            };
        }

        private Random Rand = new Random();
    }
}
