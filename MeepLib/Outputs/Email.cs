using System;
using System.Net;
using System.Net.Mail;

using SmartFormat;

using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Send message by Email
    /// </summary>
    public class Email : AMessageModule
    {
        /// <summary>
        /// Destination address
        /// </summary>
        /// <value>To.</value>
        public DataSelector To { get; set; }

        /// <summary>
        /// Sender's address
        /// </summary>
        /// <value>From.</value>
        public DataSelector From { get; set; }

        public DataSelector Subject { get; set; }

        public DataSelector Body { get; set; } = "{msg.Value}";

        /// <summary>
        /// Address of mail server
        /// </summary>
        /// <value>The server.</value>
        public DataSelector Server { get; set; }

        /// <summary>
        /// Port number of the SMTP server
        /// </summary>
        /// <value>The port.</value>
        /// <remarks>Defaults to secure (587).</remarks>
        public DataSelector Port { get; set; } = "587";

        public DataSelector SSL { get; set; } = "true";

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);

            string to = await To.SelectStringAsync(context);
            string from = await From.SelectStringAsync(context);
            string subject = await Subject.SelectStringAsync(context);
            string body = await Body.SelectStringAsync(context);
            string server = await Server.SelectStringAsync(context);
            var parsedPort = await Port.TrySelectIntAsync(context);
            var parsedSSL = await SSL.TrySelectBoolAsync(context);

            if (!parsedPort.Parsed)
            {
                logger.Warn("Invalid port number for {0}", this.Name);
                return null;
            }

            if (!parsedSSL.Parsed)
            {
                logger.Warn("No clear true/false for SSL value for {0}", this.Name);
                return null;
            }

            try
            {
                using (SmtpClient client = new SmtpClient(server, parsedPort.Value))
                {
                    client.EnableSsl = parsedSSL.Value;
                    //client.Credentials = AHostProxy.Current?.GetCredential(server,port) as ICredentialsByHost;

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
