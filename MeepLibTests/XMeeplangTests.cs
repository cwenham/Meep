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
            var textReader = new StringReader(UnsweetenedUpstream.ToUnixEndings());
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XMeeplangDownstreamReader(xmlReader);

            try
            {
                var doc = new XmlDocument();
                doc.PreserveWhitespace = true;
                doc.Load(meepReader);

                Assert.Equal(UnsweetenedUpstream.ToUnixEndings(), doc.InnerXml);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Expand sweetened Meeplang sample to show it's the same as unsweetened
        /// </summary>
        [Fact]
        public void Desweeten()
        {
            var textReader = new StringReader(SweetenedDownstream.ToUnixEndings());
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XMeeplangDownstreamReader(xmlReader);

            var doc = new XmlDocument();
            doc.Load(meepReader);

            Assert.NotNull(doc);

            var untextReader = new StringReader(UnsweetenedDownstream.ToUnixEndings());
            var unxmlReader = XmlReader.Create(untextReader);

            var undoc = new XmlDocument();
            undoc.Load(unxmlReader);

            Assert.Equal(undoc.InnerXml, doc.InnerXml);
        }

        [Fact]
        public void Deserialise()
        {
            var textReader = new StringReader(WhereTimer);
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XMeeplangDownstreamReader(xmlReader);

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

        [Fact]
        public void ShaNamespaceTest()
        {
            var textReader = new StringReader(ShaNamespace1);
            var xmlReader = XmlReader.Create(textReader);

            xmlReader.Read();
            xmlReader.Read();
            xmlReader.Read();

            string value = xmlReader.GetAttribute("sha256", "http://meep.example.com/MeepGit/V1");
            Assert.Equal("ABC123", value);
        }

        /// <summary>
        /// Sample pipeline in MeepLang without any syntax sugar
        /// </summary>
        public static string UnsweetenedUpstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline>
    <CheckSomething>
        <Timer Interval=""00:00:30"" />
    </CheckSomething>
</Pipeline>        
";

        /// <summary>
        /// Sample pipeline in MeepLang with syntax sugar usage
        /// </summary>
        public static string SweetenedUpstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline>
    <CheckSomething Interval=""00:00:30"" />
</Pipeline>
";

        public static string UnsweetenedDownstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline>
    <Localise From=""{AsJSON}"">
        <CheckSomething Localise=""{AsJSON}""/>
    </Localise>

    <Unzip Path=""/tmp"">
        <CleanSomething Unzip=""/tmp"">
            <Localise From=""{AsJSON}"">
                <CheckSomething Localise=""{AsJSON}""/>
            </Localise>
        </CleanSomething>
    </Unzip>
</Pipeline>        
";

        public static string SweetenedDownstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline>
    <CheckSomething Localise=""{AsJSON}""/>

    <CleanSomething Unzip=""/tmp"">
        <CheckSomething Localise=""{AsJSON}""/>
    </CleanSomething>
</Pipeline>
";


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

        public static string ShaNamespace1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns:g=""http://meep.example.com/MeepGit/V1""
          g:sha256=""ABC123"">
    <g:Clone Repository=""https://github.com/cwenham/Meep.git"">
        <Timer Interval=""00:30:00""/>
    </g:Clone>
</Pipeline>
";
    }
}
