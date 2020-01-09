﻿using System;
using System.Xml.Serialization;
using System.Reactive.Linq;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Fire a message every Interval
    /// </summary>
    // ToDo: Set MacroPosition to Child once we can support that, since we want
    // to support multiple Timers for the same parent, plus Timers mixed with
    // other sources.
    [Macro(Name = "Timer", DefaultProperty = "Interval", Position = MacroPosition.Upstream)]
    public class Timer : AMessageModule
    {
        /// <summary>
        /// Length of timer interval
        /// </summary>
        /// <value>The interval.</value>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// How long to wait after start until emitting messages, default is zero
        /// </summary>
        public TimeSpan DelayFor { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// How long to run until stopping, default is infinity
        /// </summary>
        public TimeSpan RunFor { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Number of times to cycle, defaults to infinite (0)
        /// </summary>
        public long Repeat { get; set; } = 0;

        private long _remaining = 0;

        /// <summary>
        /// When this timer started, in UTC
        /// </summary>
        public DateTime Started { get; private set; }

        /// <summary>
        /// Last time the timer elapsed
        /// </summary>
        /// <value>The last elapsed.</value>
        public long LastElapsed { get; private set; }

        /// <summary>
        /// Value to put in each message
        /// </summary>
        /// <value>The payload.</value>
        public DataSelector Payload { get; set; } = "{msg.Number}";

        /// <summary>
        /// Start without firing an initial message
        /// </summary>
        /// <value>True to start without initial message</value>
        /// <remarks>Most uses of this module are for trying something as soon
        /// as Meep starts, then again after an interval. To start strictly after
        /// the Interval has passed first, set to True. Defaults to false, which is
        /// a "wet" start with an initial message fired as soon as the pipeline 
        /// starts.</remarks>
        public bool DryStart { get; set; } = false;

        protected override IObservable<Message> GetMessagingSource()
        {
            if (Repeat > 0)
                _remaining = Repeat;

            IObservable<long> source = Observable.Interval(Interval);

            if (!DryStart)
                source = source.StartWith(0);

            Started = DateTime.UtcNow;

            return from seq in source
                   let message = IssueMessage(seq)
                   where message != null
                   select message;
        }

        protected virtual Message CreateMessage(long step)
        {
            var msg = new Step
            {
                Number = step,
                LastIssued = new DateTime(LastElapsed)
            };

            MessageContext context = new MessageContext(msg, this);

            msg.Value = Payload.SelectString(context);

            return msg;
        }

        private Message IssueMessage(long step)
        {
            if (RunFor > TimeSpan.Zero && Started + RunFor < DateTime.UtcNow)
                return null;

            if (Started + DelayFor > DateTime.UtcNow)
                return null;

            if (Repeat > 0)
                if (_remaining <= 0)
                    return null;
                else
                    _remaining--;

            var msg = CreateMessage(step);
            LastElapsed = msg.CreatedTicks;

            return msg;
        }
    }
}
