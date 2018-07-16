using System;

using Xunit;

using MeepLib.Filters;
using MeepLib.Messages;

namespace MeepLibTests.Filters
{
    public class WhereTests
    {
        [Fact]
        public void PlainExpression()
        {
            Where moduleA = new Where
            {
                Expr = "2 = 2"
            };

            var resultA = moduleA.HandleMessage(TextMessage);
            resultA.Wait();
            Assert.Same(TextMessage, resultA.Result);

            Where moduleB = new Where
            {
                Expr = "2 + 2 = 4"
            };

            var resultB = moduleB.HandleMessage(TextMessage);
            resultB.Wait();
            Assert.Same(TextMessage, resultB.Result);

            Where moduleC = new Where
            {
                Expr = "2 + 2 = 5"
            };

            var resultC = moduleC.HandleMessage(TextMessage);
            resultC.Wait();
            Assert.Null(resultC.Result);
        }

        [Fact]
        public void ParameterisedExpression()
        {
            Where moduleA = new Where
            {
                Expr = "ToString() = 'Foo'"
            };

            var resultA = moduleA.HandleMessage(TextMessage);
            resultA.Wait();
            Assert.Same(TextMessage, resultA.Result);

            Where moduleB = new Where
            {
                Expr = "ToInt() = 12"
            };

            var resultB = moduleB.HandleMessage(NumMessage);
            resultB.Wait();
            Assert.Same(NumMessage, resultB.Result);
        }

        public static Message TextMessage = new StringMessage
        {
            Value = "Foo"
        };

        public static Message NumMessage = new NumericMessage
        {
            Value = 12
        };
    }
}
