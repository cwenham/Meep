using System;
using System.Net.Http;

namespace MeepLib.Outputs
{
    public class Put : Post
    {
        protected override HttpMethod Method { get; set; } = HttpMethod.Put;
    }
}
