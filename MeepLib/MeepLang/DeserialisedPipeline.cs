using System;

using MeepLib.Messages;

namespace MeepLib.MeepLang
{
    /// <summary>
    /// A pipeline that has just been deserialised from Meeplang
    /// </summary>
    public class DeserialisedPipeline : Message
    {
        /// <summary>
        /// The root module (most downstream element), usually an instance of Pipeline class
        /// </summary>
        /// <value>The pipeline.</value>
        public AMessageModule Tree { get; set; }
    }
}
