using System;
using System.Collections.Generic;
using System.Text;

using MeepLib.Messages;

namespace MeepLib.Filters
{
    /// <summary>
    /// Interface for polarised filters (return message on pass, or return null on pass)
    /// </summary>
    /// <remarks>When a filter module's HandleMessage or Pipeline tests a Message for the filter condition, it must
    /// either return ThisPassedTheTest(msg) or ThisFailedTheTest(msg). Implementations of each message would then
    /// either pass the message downstream or block (return null) depending on the polarity given by BlockOnMatch.
    /// 
    /// <para>E.G.:</para>
    /// 
    /// <code>
    /// if (msg.Value == "Foo")
    ///     return ThisPassedTheTest(msg);
    /// else
    ///     return ThisFailedTheTest(msg);
    /// </code>
    /// 
    /// <para>This can make code easier to read and understand what the module's code is doing, rather than guessing 
    /// "am I blocking on a positive, like on a blacklist, or passing-through on a positive, like on a whitelist?"
    /// Instead, the module can just let the user decide by exposing BlockOnMatch as an attribute they can set in the
    /// pipeline profile.</para></remarks>
    public interface IPolarisedFilter
    {
        bool BlockOnMatch { get; }

        /// <summary>
        /// Act according to polarity when a messages passes the filter's test
        /// </summary>
        /// <returns></returns>
        /// <param name="msg">Message.</param>
        /// <remarks>Used to make filter implementations easier to read and 
        /// understand. The module would only be concerned with its test, and
        /// returns ThisPassedTheTest(msg) to let us decide if that means the
        /// message goes through or is blocked.</remarks>
        Message ThisPassedTheTest(Message msg);

        Message ThisFailedTheTest(Message msg);
    }
}
