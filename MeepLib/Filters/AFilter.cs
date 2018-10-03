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
    public class AFilter : AMessageModule
    {
        /// <summary>
        /// Whether a positive match means the message will Pass or Block
        /// </summary>
        /// <value>Either "Pass" or "Block"</value>
        /// <remarks>Defaults to "Block".</remarks>
        public string Polarity { get; set; } = "Pass";

        protected bool blockOnMatch
        {
            get
            {
                return !Polarity.Equals("PASS", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Act according to polarity when a messages passes the filter's test
        /// </summary>
        /// <returns></returns>
        /// <param name="msg">Message.</param>
        /// <remarks>Used to make filter implementations easier to read and
        /// understand. The module would only be concerned with its test, and
        /// returns ThisPassedTheTest(msg) to let us decide if that means the
        /// message goes through or is blocked.</remarks>
        protected Message ThisPassedTheTest(Message msg)
        {
            if (blockOnMatch)
                return null;
            else
                return msg;
        }

        protected Message ThisFailedTheTest(Message msg)
        {
            if (!blockOnMatch)
                return null;
            else
                return msg;
        }
    }
}
