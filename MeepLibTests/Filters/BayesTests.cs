using System;
using System.Linq;

using Xunit;

using MeepLib.Filters;
using MeepLib.Algorithms;
using MeepLib.Messages;

namespace MeepLibTests.Filters
{
    public class BayesTests
    {
        private string[] spam = new string[]
        {
            "Yellow cucumbers on sale now",
            "Act now to get yellow cucumbers",
            "Have you reserved your cucumber yet?",
            "The future is yellow",
            "15% off the best cucumbers",
            "Cool yellow cucumbers in your area",
            "Do you ever get a craving for cucumbers?",
            "It's yellow! It's cool! It's a cucumber!"
        };

        private string[] ham = new string[]
        {
            "In a tree, by a lake, there sat a pigeon.",
            "Don't forget to take your coat, it's going to be cool.",
            "Actually, I prefer yellow mustard.",
            "Get me a tuna and cucumber sandwich.",
            "Hotel's reserved, let's get a taxi.",
            "This area has bad reception.",
            "I'd like a ham sandwich, please.",
            "How do you take your coffee?"
        };

        private string spamSample = "Nobody's cucumbers beat our yellow cucumbers";

        private string hamSample = "Roger decided to play Yellow Submarine.";

        [Fact]
        public async void TrainAndFilter()
        {
            var trainer1 = new BayesTrain
            {
                Class = "Spam"
            };

            foreach (var s in spam)
                await trainer1.HandleMessage(new StringMessage
                {
                    Value = s
                });

            var trainer2 = new BayesTrain
            {
                Class = "Ham"
            };

            foreach (var h in ham)
                await trainer2.HandleMessage(new StringMessage
                {
                    Value = h
                });

            var bayes1 = new Bayes
            {
                Class = "Spam"
            };

            var msg1 = new StringMessage
            {
                Value = spamSample
            };


            //var likelySpam1 = Bayes.Prediction(msg1.Tokens, "Spam");
            //var likelyHam1 = Bayes.Prediction(msg1.Tokens, "Ham");

            //Assert.True(likelySpam1 > 0);
            //Assert.True(likelyHam1 > 0);
            //Assert.True(likelySpam1 > likelyHam1);

            //var msg2 = new StringMessage
            //{
            //    Value = hamSample
            //};

            //var likelySpam2 = Bayes.Prediction(msg2.Tokens, "Spam");
            //var likelyHam2 = Bayes.Prediction(msg2.Tokens, "Ham");

            //Assert.True(likelySpam2 > 0);
            //Assert.True(likelyHam2 > 0);
            //Assert.True(likelySpam2 < likelyHam2);
        }
    }
}
