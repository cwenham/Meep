using System;
using System.Collections.Generic;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using NLog;
using SmartFormat;

using MeepLib.MeepLang;
using MeepLib.Messages;

// CURRENTLY NOT FINISHED. We need to wait for Microsoft to issue the fix for 
// Issue #36719: NETStandard2.0 libraries referencing types absorbed into 
// NETStandard2.1 cannot be used in NS2.1
// https://github.com/dotnet/corefx/issues/36719

namespace MeepLib.Sources
{
    /// <summary>
    /// Full-duplex Web Socket
    /// </summary>
    public class WebSocket : AMessageModule
    {
        /// <summary>
        /// URI of the web socket in {Smart.Format}
        /// </summary>
        /// <remarks>Evaluated once at pipeline start, not per inbound message. msg will be null.</remarks>
        public string URI { get; set; }

        /// <summary>
        /// Template for upstream pipeline messages that are outbound on the socket, in {Smart.Format}
        /// </summary>
        /// <remarks>If we're downstream from other modules, whatever they send
        /// us will be serialised and transmitted out on the WebSocket using
        /// this template. Defaults to JSON serialised message.</remarks>
        public string OutboundTemplate { get; set; } = "{msg.AsJSON}";

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_pipeline is null)
                    _pipeline = Observable
                            .Create<Message>(observer => TaskPoolScheduler.Default
                                                        .Schedule(() => ReadWriteLoop(observer, UpstreamMessaging)))
                            .Publish().RefCount();

                return _pipeline;
            }
        }
        private IObservable<Message> _pipeline;


        private async void ReadWriteLoop(IObserver<Message> observer, IObservable<Message> outbound)
        {
            // We may want to re-write this to use IAsyncEnumerable if we can do async
            // read-and-write that way.

            MessageContext context = new MessageContext(null, this);
            string sfUri = Smart.Format(URI, context);

            while (true) // Level where retries happen
            {
                _cancelToken = new CancellationToken();

                try
                {
                    using (_socket = new ClientWebSocket())
                    {
                        var connectTask = _socket.ConnectAsync(new Uri(sfUri), _cancelToken);
                        connectTask.Wait();

                        // Start feeding outbound messages (messages given to us from upstream pipeline)
                        // ToDo: test what happens when we re-connect with a new ClientWebSocket.
                        outbound.Subscribe<Message>(x => WriteOutboundMessage(_socket, x, _cancelToken));

                        System.Memory<byte> buffer = new System.Memory<byte>();

                        while (_socket.State == WebSocketState.Open)
                        {
                            var result = await _socket.ReceiveAsync(buffer, _cancelToken);
                            
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "Error reading WebSocket");
                }

            }
        }

        private void WriteOutboundMessage(ClientWebSocket socket, Message msg, CancellationToken cancelToken)
        {
            MessageContext context = new MessageContext(msg, this);
            string sfMsg = Smart.Format(OutboundTemplate, context);

            ArraySegment<byte> buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(sfMsg));

            socket.SendAsync(buffer, WebSocketMessageType.Text, false, cancelToken);
        }

        private ClientWebSocket _socket;
        private CancellationToken _cancelToken;
    }
}
