using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.IO.Pipelines;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Linq;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Listen for incoming TCP connections
    /// </summary>
    /// <remarks>First stage of a TCP server, this converts the incoming TCP connections into TcpConnection Messages
    /// that can be collected by a downstream module that subsequently either receives or transmits messages from/to
    /// those clients.
    ///
    /// <para>Since the TcpConnection messages encapsulate a live Socket, they can be sorted and managed like any other
    /// Message by standard Meep modules until they've reached a module that knows what to say or hear from them,
    /// therefore TcpListen is usually paired with Reflect or another module before it can be useful.</para>
    ///
    /// <para>See https://github.com/cwenham/Meep/wiki/Meep-As-A-Server </para>
    /// </remarks>
    public class TcpListen : AMessageModule
    {
        /// <summary>
        /// Host name, without scheme (eg: host.domain.com or IP address)
        /// </summary>
        /// <value>The host.</value>
        /// <remarks>Defaults to localhost. {msg} will not be available here, but cfg and mdl will.</remarks>
        public DataSelector Host { get; set; }

        /// <summary>
        /// TCP port number to listen on
        /// </summary>
        /// <value>The port.</value>
        /// <remarks>{msg} will not be available here, but cfg and mdl will.
        /// <para>Defaults to 7769 ('M' and 'E' on the ASCII table).</para></remarks>
        public DataSelector Port { get; set; } = "7769";

        /// <summary>
        /// What to transmit to each connected client for each inbound message
        /// </summary>
        /// <remarks>Defaults to the message serialised to JSON.</remarks>
        public DataSelector From { get; set; } = "{msg.AsJson}";

        public override IObservable<Message> Pipeline
        {
            get
            {
                var context = new MessageContext(null, this);
                IPAddress ip = null;

                if (_listener != null)
                    _listener.Stop();

                string dsHost = null;
                if (Host is null)
                    dsHost = "localhost";

                ip = Dns.GetHostEntry(dsHost).AddressList.First();


                var dsPort = Port.TrySelectLong(context);
                if (!dsPort.Parsed)
                    throw new ArgumentException("Invalid port number");

                _listener = new TcpListener(ip, (int)dsPort.Value);
                _listener.Start();

                if (_pipeline == null)
                    _pipeline = Observable
                            .Create<Message>(observer => TaskPoolScheduler.Default
                            .Schedule(() => ConnectionToMessage(_listener, observer)));

                return _pipeline;
            }
        }
        private IObservable<Message> _pipeline;

        private TcpListener _listener;

        private bool _keepListening = true;

        /// <summary>
        /// Convert incoming TCP connections into TcpConnection messages
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="clientObserver"></param>
        /// <remarks>Listens for inbound connections and emits TcpConnection messages for each.</remarks>
        private void ConnectionToMessage(TcpListener listener, IObserver<Message> clientObserver)
        {
             while (_keepListening)
             {
                var client = listener.AcceptTcpClient();
                clientObserver.OnNext(new TcpConnection
                {
                     Client = client
                });
             }
        }

        public override void Dispose()
        {
            _keepListening = false;

            if (_listener != null)
                _listener.Stop();

            base.Dispose();
        }
    }
}
