using System;

using SmartFormat;
using SmartFormat.Core.Extensions;

namespace MeepLib.Algorithms.SmartFormatExtensions
{
    public class CSVEscape : IFormatter
    {
        private string[] names = { "CSVEscape" };
        public string[] Names { get { return names; } set { this.names = value; } }

        public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
        {
            string value = formattingInfo.CurrentValue as string;
            if (value == null)
                return false;

            formattingInfo.Write(value.Replace("\t","\\t").Replace("\n","\\n").Replace("\r","\\r"));

            return true;
        }
    }
}
