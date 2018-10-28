using System;

namespace MeepLib.Flow
{
    /// <summary>
    /// A no-op module that serves as a simple container, merging all inbound 
    /// messages to a single, tappable point
    /// </summary>
    /// <remarks>Use when you need to combine the output of several modules
    /// into one named point that you can &lt;Tap From/&gt;.</remarks>
    public class Stage : AMessageModule
    {
    }
}
