using System;

namespace MeepLib.Config
{
    /// <summary>
    /// HTTP request header
    /// </summary>
    public class Header : AConfig
    {
        /// <summary>
        /// Header value in {Smart.Format}
        /// </summary>
        /// <value>The value.</value>
        public string Value { get; set; }
    }
}
