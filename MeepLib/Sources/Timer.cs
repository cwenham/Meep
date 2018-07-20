﻿using System;
using System.Xml.Serialization;
using System.Reactive.Linq;

using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepLib.Sources
{
    [XmlRoot(ElementName = "Timer", Namespace = "http://meep.example.com/Meep/V1")]
    [Macro(Name = "Interval", DefaultProperty = "Interval", Position = MacroPosition.Child)]
    public class Timer : AMessageModule
    {
        /// <summary>
        /// Length of timer interval
        /// </summary>
        /// <value>The interval.</value>
        [XmlIgnore]
        public TimeSpan Interval { get; set; }

        [XmlAttribute(AttributeName = "Interval")]
        public string strInterval
        {
            get
            {
                return Interval.ToString();
            }
            set
            {
                Interval = TimeSpan.Parse(value);
            }
        }

        /// <summary>
        /// Last time the timer elapsed
        /// </summary>
        /// <value>The last elapsed.</value>
        [XmlIgnore]
        public long LastElapsed { get; private set; }

        /// <summary>
        /// Value in {Smart.Format} to put in each message
        /// </summary>
        /// <value>The payload.</value>
        [XmlAttribute]
        public string Payload { get; set; } = "{msg.Number}";

        [XmlIgnore]
        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_Pipeline == null)
                    _Pipeline = from seq in Observable.Interval(Interval)
                                let message = IssueMessage(seq)
                                where message != null
                                select message;

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
