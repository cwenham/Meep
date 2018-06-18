using System;
using System.Net;
using System.Net.Mail;

using SmartFormat;

using MeepModel.Messages;
using System.Threading.Tasks;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Send message by Email
    /// </summary>
    public class Email : AMessageModule
    {
        /// <summary>
        /// Destination address in {Smart.Format}
        /// </summary>
        /// <value>To.</value>
        public string To { get; set; }

        /// <summary>
        /// Sender's address in {Smart.Format}
        /// </summary>
        /// <value>From.</value>
        public string From { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; } = "{msg.Value}";

        /// <summary>
        /// Address of mail server in {Smart.Format}
        /// </summary>
        /// <value>The server.</value>
        public string Server { get; set; }

        /// <summary>
        /// Port of the SMTP server in {Smart.Format}
        /// </summary>
        /// <value>The port.</value>
        public string Port { get; set; } = "25";

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            string to = Smart.Format(To, context);
            string from = Smart.Format(From, context);
            string subject = Smart.Format(Subject, context);
            string body = Smart.Format(Body, context);
            string server = Smart.Format(Server, context);
            int port = int.Parse(Smart.Format(Port, context));

            using (SmtpClient client = new SmtpClient(server, port))
            {
                client.Port = port;

                MailMessage message = new MailMessage(from, to, subject, body);
                await client.SendMailAsync(message);
            }

            return msg;
        }
    }
}
