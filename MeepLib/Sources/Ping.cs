using System;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Xml.Serialization;

using SmartFormat;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    [XmlRoot(ElementName = "Ping", Namespace = "http://meep.example.com/Meep/V1")]
    public class Ping : AMessageModule
    {
        public Ping()
        {
            // Set default timeout
            Timeout = TimeSpan.FromMilliseconds(3000);
        }

        /// <summary>
        /// Address of host to ping, in {Smart.Format}
        /// </summary>
        /// <value>To.</value>
        [XmlAttribute]
        public string To { get; set; }

        /// <summary>
        /// Padding added to response time, for hosts less than 1ms away (same machine or rack)
        /// </summary>
        /// <value>The padding.</value>
        /// <remarks>The main use is to distinguish between no-response and a sub-milisecond
        /// response on health monitoring systems that ignore error messages and just display 
        /// a number, like a packed Telemetry dashboard.</remarks>
        [XmlIgnore]
        public TimeSpan Padding { get; set; } = TimeSpan.Zero;

        [XmlAttribute(AttributeName = "Padding")]
        public string strPadding
        {
            get
            {
                return Padding.ToString();
            }
            set
            {
                Padding = TimeSpan.Parse(value);
            }
        }

        private System.Net.NetworkInformation.Ping Pinger { get; set; }

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            string pingTo = Smart.Format(To, context);

            if (Pinger == null)
                Pinger = new System.Net.NetworkInformation.Ping();

            var response = await Pinger.SendPingAsync(pingTo, (int)Timeout.TotalMilliseconds);

            if (response.Status == IPStatus.Success)
                return new Step
                {
                    DerivedFrom = msg,
                    Number = ((Step)msg)?.Number ?? 0,
                    Value = response.RoundtripTime + Padding.TotalMilliseconds
                };
            else
                return new Message
                {
                    DerivedFrom = msg,
                    Value = response.Status.ToString()
                };
        }
    }
}
