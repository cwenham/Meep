using System;
using System.Collections.Generic;
using System.Text;

using Newtonsoft.Json.Linq;

namespace MeepLib.Messages
{
    /// <summary>
    /// A parsed JSON token as a JSON.Net JToken
    /// </summary>
    public class JTokenMessage : Message
    {
        public JToken Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
