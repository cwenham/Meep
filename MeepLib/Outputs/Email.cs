using System;
using System.Net;
using System.Net.Mail;
using System.Xml.Serialization;

using SmartFormat;

using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Send message by Email
    /// </summary>
    [XmlRoot(ElementName = "Email", Namespace = "http://meep.example.com/Meep/V1")]
    public class Email : AMessageModule
    {
        /// <summary>
        /// Destination address in {Smart.Format}
        /// </summary>
        /// <value>To.</value>
        [XmlAttribute]
        public string To { get; set; }

        /// <summary>
        /// Sender's address in {Smart.Format}
        /// </summary>
        /// <value>From.</value>
        [XmlAttribute]
        public string From { get; set; }

        [XmlAttribute]
        public string Subject { get; set; }

        [XmlAttribute]
        public string Body { get; set; } = "{msg.Value}";

        /// <summary>
        /// Address of mail server in {Smart.Format}
        /// </summary>
        /// <value>The server.</value>
        [XmlAttribute]
        public string Server { get; set; }

        /// <summary>
        /// Port of the SMTP server in {Smart.Format}
        /// </summary>
        /// <value>The port.</value>
        /// <remarks>Defaults to secure (587).</remarks>
        [XmlAttribute]
        public string Port { get; set; } = "587";

        [XmlAttribute]
        public bool SSL { get; set; } = true;

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            string to = Smart.Format(To, context);
            string from = Smart.Format(From, context);
            string subject = Smart.Format(Subject, context);
            string body = Smart.Format(Body, context);
            string server = Smart.Format(Server, context);
            int port = int.Parse(Smart.Format(Port, context));

            try
            {
                using (SmtpClient client = new SmtpClient(server, port))
                {
                    client.EnableSsl = SSL;
                    client.Credentials = AHostProxy.Current?.GetCredential(server,port) as ICredentialsByHost;

                    MailMessage message = new MailMessage(from, to, subject, body);
                    await client.SendMailAsync(message);
                }

                return msg;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "{0} thrown when sending mail: {1}", ex.GetType().Name, ex.Message);
                return null;
            }
        }
    }
}
