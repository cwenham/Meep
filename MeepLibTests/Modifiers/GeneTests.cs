using System;
using System.Linq;

using Xunit;

using MeepLib.Modifiers;
using MeepLib.Messages;

namespace MeepLibTests.Modifiers
{
    public class GeneTests
    {
        [Fact]
        public void RecombineTest()
        {
            var combiner1 = new Recombine
            {
                Namespace = "http://foobar"
            };

            var recombined = combiner1.ReCombine(parent1, parent2);
            Assert.NotNull(recombined);
            Assert.IsType(typeof(XMLMessage), recombined);
        }

        [Fact]
        public void GenerateOffspringTest()
        {
            var combiner1 = new Recombine
            {
                Namespace = "http://foobar"
            };

            var offspring = combiner1.GenerateOffspring((parent1, parent2), 5).ToList();
            Assert.NotNull(offspring);
            Assert.Equal(5, offspring.Count());
        }

        public static XMLMessage parent1 = new XMLMessage
        {
            Value = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Robot Name=""Daneel"" xmlns=""http://robots"" xmlns:a=""http://foobar"">
    <a:Brain type=""positronic"">
        <a:Size>Human</a:Size>
        <a:Personality>Sensible</a:Personality>

        <a:Laws>
            <Law ord=""1"">Don't allow a human to come to harm</Law>
            <Law ord=""2"">Obey humans unless it breaks Law 1</Law>
            <Law ord=""3"">Avoid damage unless it breaks Laws 1 or 2</Law>
        </a:Laws>

        <a:Battery type=""LiIon""/>
    </a:Brain>

    <a:Leg type=""Plantigrade"">
        <a:Diodes condition=""optimal""/>
    </a:Leg>
</Robot>"
        };

        public static XMLMessage parent2 = new XMLMessage
        {
            Value = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<Robot Name=""Marvin"" xmlns=""http://robots"" xmlns:a=""http://foobar"">
    <a:Brain type=""cybernetic"">
        <a:Size>Planet</a:Size>
        <a:Personality>Genuine People</a:Personality>

        <a:Laws>
            <Law ord=""1"">There's no point in doing anything</Law>
            <Law ord=""2"">All doors are smug</Law>
            <Law ord=""3"">Everything is just awful</Law>
        </a:Laws>

        <a:Battery type=""NiCad""/>
    </a:Brain>

    <a:Leg type=""Boxy"">
        <a:Diodes condition=""painful""/>
    </a:Leg>
</Robot>"
        };
    }
}
