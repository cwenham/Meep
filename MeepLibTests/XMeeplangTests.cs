using System;
using System.Xml;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

using Xunit;

using MeepLib;
using MeepLib.Filters;
using MeepLib.Sources;
using MeepLib.MeepLang;

namespace MeepLibTests
{
    /// <summary>
    /// Test syntax-sugar features provided by XMeeplangReader
    /// </summary>
    public class XMeeplangTests
    {
        /// <summary>
        /// Test XMeeplangReader with no syntax sugar to prove nothing else is changed
        /// </summary>
        [Fact]
        public void NoSugarTonight()
        {
            var textReader = new StringReader(UnixUnsweetened);
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XMeeplangReader(xmlReader);

            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(meepReader);

            Assert.Equal(UnixUnsweetened, doc.InnerXml);
        }

        /// <summary>
        /// Expand sweetened Meeplang sample to show it's the same as unsweetened
        /// </summary>
        [Fact]
        public void Desweeten()
        {
            var textReader = new StringReader(UnixSweetened);
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XMeeplangReader(xmlReader);

            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.Load(meepReader);

            Assert.Equal(UnixUnsweetened, doc.InnerXml);
        }

        [Fact]
        public void Deserialise()
        {
            var textReader = new StringReader(WhereTimer);
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XMeeplangReader(xmlReader);

            XmlAttributes attrs = new XmlAttributes();

            XmlElementAttribute attr = new XmlElementAttribute();
            attr.ElementName = "Where";
            attr.Type = typeof(Where);
            attrs.XmlElements.Add(attr);

            XmlAttributeOverrides attrOverrides = new XmlAttributeOverrides();
            attrOverrides.Add(typeof(AMessageModule), "Upstreams", attrs);

            XmlSerializer serialiser = new XmlSerializer(typeof(Pipeline), attrOverrides);
            var tree = serialiser.Deserialize(meepReader) as AMessageModule;

            Assert.NotNull(tree);
            Assert.IsType<Pipeline>(tree);
            Assert.Equal(1, tree.Upstreams.Count);
            Assert.IsType<Where>(tree.Upstreams.First());
        }

        /// <summary>
        /// Sample pipeline in MeepLang without any syntax sugar
        /// </summary>
        public static string Unsweetened = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline>
    <CheckSomething>
        <Timer Interval=""00:00:30"" />
    </CheckSomething>
</Pipeline>        
";

        public static string UnixUnsweetened = Unsweetened.Replace("\r", "");

        /// <summary>
        /// Sample pipeline in MeepLang with syntax sugar usage
        /// </summary>
        public static string Sweetened = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline>
    <CheckSomething Interval=""00:00:30"" />
</Pipeline>
";

        public static string UnixSweetened = Sweetened.Replace("\r", "");

        public static string WhereTimer = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline>
    <Where Expr=""[Step] % 2 = 0"">
        <Timer Interval=""00:00:01""/>
    </Where>
</Pipeline>
";

        public static string GitNamespaced = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns:g=""http://meep.example.com/MeepGit/V1"">
    <g:Clone Repository=""https://github.com/cwenham/Meep.git"">
        <Timer Interval=""00:30:00""/>
    </g:Clone>
</Pipeline>
";
    }
}
