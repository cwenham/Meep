using System;

using MeepLib.Messages;

namespace MeepLib.Filters
{
    /// <summary>
    /// Base class for filters
    /// </summary>
    /// <remarks>Subclasses are expected to check blockOnMatch to control behavior
    /// in case of a match. The user will optionally set the value of Polarity
    /// if they want to flip the action of the filter in reverse; to pass
    /// messages that match instead of block them.</remarks>
    public class AFilter : AMessageModule, IPolarisedFilter
    {
        /// <summary>
        /// Whether a positive match means the message will Pass or Block
        /// </summary>
        /// <value>Either "Pass" or "Block"</value>
        /// <remarks>Defaults to "Block".</remarks>
        public string Polarity { get; set; } = "Pass";

        public bool BlockOnMatch
        {
            get
            {
                return !Polarity.Equals("PASS", StringComparison.OrdinalIgnoreCase);
            }
        }

        public Message ThisPassedTheTest(Message msg)
        {
            if (BlockOnMatch)
                return null;
            else
                return msg;
        }

        public Message ThisFailedTheTest(Message msg)
        {
            if (!BlockOnMatch)
                return null;
            else
                return msg;
        }
    }
}
