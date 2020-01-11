using System;
using System.Reactive.Linq;

using MeepLib.Messages;
using MeepLib.Messages.Exceptions;
using MeepLib.MeepLang;

namespace MeepLib.Flow
{
    /// <summary>
    /// Delay each Message for a fixed period of time
    /// </summary>
    public class Delay : AMessageModule
    {
        /// <summary>
        /// How long to delay Messages, as a selector that evaluates to a TimeSpan. Defaults to 10 seconds
        /// </summary>
        /// <remarks>Selector cannot reference <code>msg</code> because it's evaluated before messages are received.
        /// </remarks>
        public DataSelector By { get; set; } = "00:00:10";

        /// <summary>
        /// How long to delay messages, as a selector that evaluates to milliseconds
        /// </summary>
        /// <remarks>Selector cannot reference <code>msg</code> because it's evaluated before messages are received.
        /// </remarks>
        public DataSelector ByMS { get; set; }

        protected override IObservable<Message> GetMessagingSource()
        {
            MessageContext context = new MessageContext(null, this);

            TimeSpan delay = TimeSpan.Zero;
            if (ByMS != null)
                delay = TimeSpan.FromMilliseconds(ByMS.TrySelectLong(context).Value);
            else
                delay = TimeSpan.Parse(By.SelectString(context));

            return base.GetMessagingSource().Delay(delay);
        }
    }
}
