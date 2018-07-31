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
    }
}
