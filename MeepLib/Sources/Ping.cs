﻿using System;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    public class Ping : AMessageModule
    {
        public Ping()
        {
            // Set default timeout
            Timeout = TimeSpan.FromMilliseconds(3000);
        }

        /// <summary>
        /// Address of host to ping
        /// </summary>
        /// <value>To.</value>
        public DataSelector To { get; set; }

        /// <summary>
        /// Padding added to response time, for hosts less than 1ms away (same machine or rack)
        /// </summary>
        /// <value>The padding.</value>
        /// <remarks>The main use is to distinguish between no-response and a sub-milisecond
        /// response on health monitoring systems that ignore error messages and just display 
        /// a number, like a packed Telemetry dashboard.</remarks>
        public TimeSpan Padding { get; set; } = TimeSpan.Zero;

        private System.Net.NetworkInformation.Ping Pinger { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string pingTo = await To.SelectStringAsync(context);

            if (Pinger == null)
                Pinger = new System.Net.NetworkInformation.Ping();

            var response = await Pinger.SendPingAsync(pingTo, (int)Timeout.TotalMilliseconds);

            if (response.Status == IPStatus.Success)
                return new Step
                {
                    DerivedFrom = msg,
                    Number = ((Step)msg)?.Number ?? 0,
                    Duration = TimeSpan.FromMilliseconds(response.RoundtripTime) + Padding,
                    LastIssued = ((Step)msg)?.LastIssued ?? DateTime.Now
                };
            else
                return new StringMessage
                {
                    DerivedFrom = msg,
                    Value = response.Status.ToString()
                };
        }
    }
}
