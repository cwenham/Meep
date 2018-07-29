using System;

namespace MeepLib.Config
{
    public class AppKey : AConfig
    {
        public string ClientID { get; set; }

        public string ClientSecret { get; set; }

        public string RedirectUri { get; set; }
    }
}
