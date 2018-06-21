using System;

namespace MeepLib.Messages
{
    public class Step : Message
    {
        public Step()
        {
        }

        /// <summary>
        /// Step number in a sequence
        /// </summary>
        /// <value>The number.</value>
        public long Number { get; set; }

        /// <summary>
        /// Last time a step was generated from the same source
        /// </summary>
        /// <value>The last issued.</value>
        public DateTime LastIssued { get; set; }
    }
}
