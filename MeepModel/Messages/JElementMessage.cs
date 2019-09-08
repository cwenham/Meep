using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// A parsed JSON element
    /// </summary>
    public class JElementMessage : Message
    {
        public JsonElement Value { get; set; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
