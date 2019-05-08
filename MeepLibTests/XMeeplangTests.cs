using System;
using System.Xml;
using System.Text;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Reflection;

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
            var meepReader = new XDownstreamReader(xmlReader);

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
        public void DesweetenDownstream()
        {
            var textReader = new StringReader(SweetenedDownstream.ToUnixEndings());
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XDownstreamReader(xmlReader);

            var doc = new XmlDocument();
            doc.Load(meepReader);

            Assert.NotNull(doc);

            var untextReader = new StringReader(UnsweetenedDownstream.ToUnixEndings());
            var unxmlReader = XmlReader.Create(untextReader);

            var undoc = new XmlDocument();
            undoc.Load(unxmlReader);

            Assert.Equal(undoc.InnerXml, doc.InnerXml);
        }

        public static string UnsweetenedDownstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns=""http://meep.example.com/Meep/V1""
    xmlns:s=""http://meep.example.com/MeepSQL/V1"">
    <Localise From=""One"">
        <CheckSomethingOne Localise=""One""/>
    </Localise>

    <Unzip Path=""/tmp"">
        <CleanSomething Unzip=""/tmp"">
            <Localise From=""Two"">
                <CheckSomethingTwo Localise=""Two"">
                    <Foo/>
                </CheckSomethingTwo>
            </Localise>
        </CleanSomething>
    </Unzip>

    <s:InsertOrReplace DBTable=""MyDB:Widgets"">
        <FetchFromSomewhere s:Store=""MyDB:Widgets""/>
    </s:InsertOrReplace>

    <Foo/>
    <Bar/>
</Pipeline>        
";

        public static string SweetenedDownstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns=""http://meep.example.com/Meep/V1""
    xmlns:s=""http://meep.example.com/MeepSQL/V1"">
    <CheckSomethingOne Localise=""One""/>

    <CleanSomething Unzip=""/tmp"">
        <CheckSomethingTwo Localise=""Two"">
            <Foo/>
        </CheckSomethingTwo>
    </CleanSomething>

    <FetchFromSomewhere s:Store=""MyDB:Widgets""/>

    <Foo/>
    <Bar/>
</Pipeline>
";

        [Fact]
        public void DesweetenUpstream()
        {
            var textReader = new StringReader(SweetenedUpstream.ToUnixEndings());
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XUpstreamReader(xmlReader);

            var doc = new XmlDocument();
            doc.Load(meepReader);

            Assert.NotNull(doc);

            var untextReader = new StringReader(UnsweetenedUpstream.ToUnixEndings());
            var unxmlReader = XmlReader.Create(untextReader);

            var undoc = new XmlDocument();
            undoc.Load(unxmlReader);

            Assert.Equal(undoc.InnerXml, doc.InnerXml);
        }

        /// <summary>
        /// Sample pipeline in MeepLang without any syntax sugar
        /// </summary>
        public static string UnsweetenedUpstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns=""http://meep.example.com/Meep/V1"">
    <CheckSomething Where=""[Number] == 2"">
        <Where Expr=""[Number] == 2"">
            <Timer Interval=""00:00:30"" />
        </Where>
    </CheckSomething>
</Pipeline>        
";

        /// <summary>
        /// Sample pipeline in MeepLang with syntax sugar usage
        /// </summary>
        public static string SweetenedUpstream = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns=""http://meep.example.com/Meep/V1"">
    <CheckSomething Where=""[Number] == 2"">
        <Timer Interval=""00:00:30""/>
    </CheckSomething>
</Pipeline>
";


        [Fact]
        public void Deserialise()
        {
            var textReader = new StringReader(WhereTimer);
            var xmlReader = XmlReader.Create(textReader);
            var meepReader = new XDownstreamReader(xmlReader);

            var deserialiser = new XMeeplangDeserialiser();
            var tree = deserialiser.Deserialise(meepReader);

            Assert.NotNull(tree);
            Assert.IsType<Pipeline>(tree);
            Assert.Equal(1, tree.Upstreams.Count);
            Assert.IsType<Where>(tree.Upstreams.First());
            Assert.IsType<Timer>(tree.Upstreams.First().Upstreams.First());

            var timer = tree.Upstreams.First().Upstreams.First() as Timer;
            Assert.Equal(14, timer.Interval.TotalSeconds);
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


        public static string WhereTimer = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns=""http://meep.example.com/Meep/V1"">
    <Where Expr=""[Step] % 2 = 0"">
        <Timer Interval=""00:00:14""/>
    </Where>
</Pipeline>
";

        public static string GitNamespaced = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns=""http://meep.example.com/Meep/V1""
    xmlns:g=""http://meep.example.com/MeepGit/V1"">
    <g:Clone Repository=""https://github.com/cwenham/Meep.git"">
        <Timer Interval=""00:30:00""/>
    </g:Clone>
</Pipeline>
";

        public static string ShaNamespace1 = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Pipeline xmlns=""http://meep.example.com/Meep/V1""
    xmlns:g=""http://meep.example.com/MeepGit/V1""
    g:sha256=""ABC123"">
    <g:Clone Repository=""https://github.com/cwenham/Meep.git"">
        <Timer Interval=""00:30:00""/>
    </g:Clone>
</Pipeline>
";
    }
}
