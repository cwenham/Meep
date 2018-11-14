using System;
using System.Linq;

using Xunit;

using MeepLib.Modifiers;
using MeepLib.Messages;

namespace MeepLibTests.Modifiers
{
    public class SplitTests
    {
        [Fact]
        public async void CSVParse()
        {
            Split split1 = new Split
            {
                On = ",",
                Columns = "*"
            };

            Message result = await split1.HandleMessage(new StringMessage
            {
                Value = SampleCSV1
            });

            Assert.NotNull(result);
            Assert.IsType(typeof(Batch), result);

            Batch movies = result as Batch;
            Assert.Equal(3, movies.Messages.Count());
            Assert.IsType(typeof(RecordMessage), movies.Messages.First());

            RecordMessage movie1 = movies.Messages.First() as RecordMessage;
            Assert.IsType(typeof(TimeSpan), movie1.Record["Runtime"]);
            Assert.IsType(typeof(DateTime), movie1.Record["Released"]);
            Assert.IsType(typeof(int), movie1.Record["Rating"]);
        }

        public static string SampleCSV1 = @"
Title,Director,Runtime,Released,Rating,Review
Sunshine,Danny Boyle,01:47:33,2007-4-6 12:00:00,2,Emos in space
Fargo,Joel and Ethan Coen,01:38:14,1996-3-8 14:30:00,3,I just think I'm gonna barf
2001: A Space Odyssey,Stanley Kubrick,02:22:00,1968-4-2 13:00:00,1,Nice screensaver";
    }
}
