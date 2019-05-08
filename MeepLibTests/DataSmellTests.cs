using System;
using System.Collections.Generic;
using System.Text;

using Xunit;

using MeepLib;

namespace MeepLibTests
{
    /// <summary>
    /// Test the "SmellsLike()" function for giving an educated guess of a serialised data structure's syntax
    /// </summary>
    public class DataSmellTests
    {
        [Fact]
        public void SmellsLikeNumbers()
        {
            string longintSmell = "123456789098765432";

            Assert.Equal(DataScent.Integer, longintSmell.SmellsLike());

            string decimalSmell = "39483.55643234";

            Assert.Equal(DataScent.Decimal, decimalSmell.SmellsLike());

            string fnord = "FNORD";

            Assert.Equal(DataScent.Unknown, fnord.SmellsLike());
        }

        [Fact]
        public void SmellsLikeXML()
        {
            string NotXML = "<OhYouWishIWasXML>";

            Assert.NotEqual(DataScent.XML, NotXML.SmellsLike());

            // Although empty, technically it's XML by declaration
            string EasyMinimalXML = "<?xml version=\"1.0\" encoding=\"utf - 8\"?>";

            Assert.Equal(DataScent.XML, EasyMinimalXML.SmellsLike());

            string EasySmallXML = "<?xml version=\"1.0\" encoding=\"utf - 8\"?><Doop></Doop>";

            Assert.Equal(DataScent.XML, EasySmallXML.SmellsLike());

            string NoDeclarationXML = "<MatchingTag>\nStuff here\n</MatchingTag>";

            Assert.Equal(DataScent.XML, NoDeclarationXML.SmellsLike());
        }

        [Fact]
        public void SmellsLikeJSON()
        {
            string notJSON = "{Har.De.Har.Im.Actually.SmartFormat}";

            Assert.NotEqual(DataScent.JSON, notJSON.SmellsLike());

            string isJSON = "[{\"Record1\": 123}]";

            Assert.Equal(DataScent.JSON, isJSON.SmellsLike());
        }

        [Fact]
        public void SmellsLikeXPath()
        {
            string notXPath = "/var/temp/f00.txt";

            Assert.NotEqual(DataScent.XPath, notXPath.SmellsLike());

            string isXPath = "//Element[@name='Hydrogen']/Personality/@FavouriteBeatle";

            Assert.Equal(DataScent.XPath, isXPath.SmellsLike());
        }

        [Fact]
        public void SmellsLikeJsonPath()
        {
            string notJPath = "//Element[@name='Hydrogen']/Personality/@FavouriteBeatle";

            Assert.NotEqual(DataScent.JsonPath, notJPath.SmellsLike());

            string isJPath = "$.packages.richtracks[0].contributions[0].name";

            Assert.Equal(DataScent.JsonPath, isJPath.SmellsLike());
        }


        // No, there isn't a SmellsLikeTeenSpirit or SmellsLikeNirvana joke in here. Go away.
    }
}
