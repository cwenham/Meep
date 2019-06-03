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
        /// Body of response in {Smart.Format}
        /// </summary>
        /// <value></value>
        public string Body { get; set; }

        /// <summary>
        /// MIME type for response
        /// </summary>
        /// <value>The type of the content.</value>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// HTTP response code
        /// </summary>
        /// <value>The status code.</value>
        public string StatusCode { get; set; } = "200";

        public override async Task<Message> HandleMessage(Message msg)
        {
            // Find the original HTTP request, it'll have the context to respond on.
            WebRequestMessage entry = msg.FirstAncestor<WebRequestMessage>();
            if (entry == null
                || entry.Context == null
                || !entry.Context.Response.OutputStream.CanWrite)
                return msg;

            try
            {
                int status = int.Parse(Smart.Format(StatusCode, msg));
                var contentType = Smart.Format(ContentType, msg);

                var context = entry.Context;
                context.Response.StatusCode = status;
                context.Response.ContentType = contentType;

                StreamMessage smsg = msg as StreamMessage;
                if (smsg != null && Body is null)
                {
                    var outStream = await smsg.GetStream();
                    if (outStream.CanSeek)
                        outStream.Position = 0;
                    outStream.CopyTo(context.Response.OutputStream);                    
                }
                else
                {
                    var payload = Smart.Format(Body ?? "{AsJSON}", msg);
                    var payloadBytes = Encoding.UTF8.GetBytes(payload);
                    context.Response.ContentLength64 = payloadBytes.Length;
                    context.Response.OutputStream.Write(payloadBytes, 0, payloadBytes.Length);
                }

                context.Response.Close();
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
