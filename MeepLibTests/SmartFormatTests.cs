using System;

using Xunit;
using SmartFormat;

using MeepLib;
using MeepLib.Config;
using MeepLib.MeepLang;

namespace MeepLibTests
{
    public class SmartFormatTests
    {
        [Fact]
        public void NamedConfig()
        {
            AppKey key = new AppKey
            {
                Name = "Test1",
                ClientID = "Tarquin Fintin Limbim",
                ClientSecret = "Bar",
                RedirectUri = "http://127.0.0.1:8077/"
            };

            MessageContext context1 = new MessageContext(null, null);

            string config1 = "{cfg.Test1.ClientID}";
            string formatted1 = Smart.Format(config1, context1);

            Assert.Equal("Tarquin Fintin Limbim", formatted1);
        }

        [Fact]
        public void ParameterisedQuery()
        {
            string template = "UPDATE foo SET bar = {msg.bar}, baz = {msg.child.baz} WHERE fnord = {msg.record.fnord}";

            string[] parameterised = template.ToSmartParameterised();
            Assert.Equal("UPDATE foo SET bar = @arg1, baz = @arg2 WHERE fnord = @arg3", parameterised[0]);
            Assert.Equal("{msg.bar}", parameterised[1]);
            Assert.Equal("{msg.child.baz}", parameterised[2]);
            Assert.Equal("{msg.record.fnord}", parameterised[3]);
            Assert.Equal(4, parameterised.Length);
        }
    }
}
