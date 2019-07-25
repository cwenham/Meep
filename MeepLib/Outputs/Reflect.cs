using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Buffers;

using MeepLib.Messages;

namespace MeepLib.Outputs
{
    /// <summary>
    /// Reflect messages to a collection of Connections
    /// </summary>
    /// <remarks>This usually takes two upstream message sources, or a mix that ultimately derived from at least one
    /// TcpListen module and something that generates data that you want to reflect to connected clients (unless its the
    /// fact of an incoming client connection that you want to transmit to all the other clients).
    ///
    /// <para>Therefore, this will give special treatment to any TcpConnection message it receives from upstream,
    /// caching it in a collection. When any other kind of message type arrives, it will iterate through the collection
    /// and copy the same message to each of them.</para>
    ///
    /// <para>A chat room is a typical example: each incoming message from each client is reflected out to all of the
    /// other clients in turn. The fact of connections from new clients (an announcement to everyone that someone new
    /// has joined the chat) would be done by consuming TcpConnections downstream, converting them into the
    /// announcement, and then feeding them back into this module with <see cref="Tap"/>.</para>
    ///
    /// <para>Clients are dropped from the collection when their connections are terminated.</para>
    /// </remarks>
    public class Reflect : AMessageModule
    {
        /// <summary>
        /// What to transmit to each client in the connection
        /// </summary>
        public DataSelector From { get; set; } = "{msg.AsJSON}";

        public override async Task<Message> HandleMessage(Message msg)
        {
            IConnectionMessage cMsg = msg as IConnectionMessage;
            if (cMsg != null)
                _clients.Add(new ReflectClient(cMsg));
            else
            {
                MessageContext context = new MessageContext(msg, this);
                ReadOnlyMemory<byte> dsContent = await From.SelectROMByteAsync(context);

                var outbounds = from c in _clients
                                let task = c.Transmit(dsContent, _cancelSource.Token)
                                select task;
            }

            return msg;
        }

        private ConcurrentBag<ReflectClient> _clients = new ConcurrentBag<ReflectClient>();

        private CancellationTokenSource _cancelSource = new CancellationTokenSource();

        public override void Dispose()
        {
            _cancelSource.Cancel();
            base.Dispose();
        }
    }

    /// <summary>
    /// Capture the state of an IConnection message, which is what we operate on to respond to connecting clients
    /// </summary>
    internal class ReflectClient
    {
        public ReflectClient(IConnectionMessage connectionMsg)
        {
            this.Message = connectionMsg;
        }

        public IConnectionMessage Message { get; set; }

        public DateTime LastTransmit { get; set; }

        /// <summary>
        /// Send a string message to a TcpClient
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public ValueTask<int> Transmit(ReadOnlyMemory<byte> message, CancellationToken cancelToken)
        {
            if (!Message.Connected)
                return new ValueTask<int>(0);

            LastTransmit = DateTime.Now;
            return Message.SendAsync(message, cancelToken);
        }
    }
}
