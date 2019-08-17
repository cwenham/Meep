using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

using SmartFormat;
using NLog;

using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Send a response to an HttpListenContext
    /// </summary>
    /// <remarks>This completes the cycle of an HTTP request along with the
    /// Listen module.</remarks>
    public class Respond : AMessageModule, IDisposable
    {
        /// <summary>
        /// Body of response
        /// </summary>
        /// <value></value>
        public DataSelector Body { get; set; }

        /// <summary>
        /// MIME type for response
        /// </summary>
        /// <value>The type of the content.</value>
        public DataSelector ContentType { get; set; } = "application/json";

        /// <summary>
        /// HTTP response code
        /// </summary>
        /// <value>The status code.</value>
        public DataSelector StatusCode { get; set; } = "200";

        public override async Task<Message> HandleMessage(Message msg)
        {
            // Find the original HTTP request, it'll have the context to respond on.
            WebRequestMessage entry = msg.FirstAncestor<WebRequestMessage>();
            if (entry == null
                || entry.Context == null
                || !entry.Context.Response.OutputStream.CanWrite)
                return msg;

            MessageContext context = new MessageContext(msg, this);

            var parsedStatus = await StatusCode.TrySelectIntAsync(context);
            if (!parsedStatus.Parsed)
            {
                logger.Warn("Failed to parse HTTP status for {0}", this.Name);
                return null;
            }

            string contentType = await ContentType.SelectStringAsync(context);

            try
            {
                var webContext = entry.Context;
                webContext.Response.StatusCode = parsedStatus.Value;
                webContext.Response.ContentType = contentType;

                StreamMessage smsg = msg as StreamMessage;
                if (smsg != null && Body is null)
                {
                    var outStream = await smsg.GetStream();
                    if (outStream.CanSeek)
                        outStream.Position = 0;
                    outStream.CopyTo(webContext.Response.OutputStream);                    
                }
                else
                {
                    var payload = await Body.SelectStringAsync(context);
                    var payloadBytes = Encoding.UTF8.GetBytes(payload);
                    webContext.Response.ContentLength64 = payloadBytes.Length;
                    webContext.Response.OutputStream.Write(payloadBytes, 0, payloadBytes.Length);
                }

                webContext.Response.Close();
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "Error sending HTTP response");
            }

            return msg;
        }

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();

        public void Dispose()
        {
            _cancelSource?.Cancel();
        }
    }
}
