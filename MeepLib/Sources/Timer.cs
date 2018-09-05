﻿using System;
using System.Xml.Serialization;
using System.Reactive.Linq;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    [Macro(Name = "Interval", DefaultProperty = "Interval", Position = MacroPosition.Child)]
    public class Timer : AMessageModule
    {
        /// <summary>
        /// Length of timer interval
        /// </summary>
        /// <value>The interval.</value>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// Last time the timer elapsed
        /// </summary>
        /// <value>The last elapsed.</value>
        public long LastElapsed { get; private set; }

        /// <summary>
        /// Value in {Smart.Format} to put in each message
        /// </summary>
        /// <value>The payload.</value>
        public string Payload { get; set; } = "{msg.Number}";

        /// <summary>
        /// Start without firing an initial message
        /// </summary>
        /// <value>True to start without initial message</value>
        /// <remarks>Most uses of this module are for trying something as soon
        /// as Meep starts, then again after an interval. If the initial starting
        /// message isn't wanted, set this to true. Defaults to false, which is
        /// a "wet" start with an initial message fired as soon as the pipeline 
        /// starts.</remarks>
        public bool DryStart { get; set; } = false;

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_Pipeline == null)
                {
                    IObservable<long> source = Observable.Interval(Interval);

                    if (!DryStart)
                        source = source.StartWith(0);

                    _Pipeline = from seq in source
                                let message = IssueMessage(seq)
                                where message != null
                                select message;
                }

                return _Pipeline;
            }
            protected set => base.Pipeline = value;
        }
        private IObservable<Message> _Pipeline;

        public virtual Message CreateMessage(long step)
        {
            var msg = new Step
            {
                Number = step,
                LastIssued = new DateTime(LastElapsed)
            };

            MessageContext context = new MessageContext(msg, this);

            msg.Value = Smart.Format(Payload, context);

            return msg;
        }

        private Message IssueMessage(long step)
        {
            var msg = CreateMessage(step);
            LastElapsed = msg.CreatedTicks;

            return msg;
        }
    }
}
