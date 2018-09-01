using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Reactive.Linq;

using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Connect and listen to a remote service with a simple TCP connection
    /// </summary>
    /// <remarks>Often used for realtime feeds of prices, scores, telemetry,
    /// and other "live" sources. These eschew the overhead of an 
    /// application-layer protocol like HTTP, so we just convert what we receive 
    /// into raw string messages*.
    /// 
    /// <para>* - if the inbound message can be deserialised into a Meep Message
    /// or known subclass, it will.</para></remarks>
    public class TcpClient : AMessageModule
    {
        /// <summary>
        /// Host name, without scheme (eg: host.domain.com or IP address)
        /// </summary>
        /// <value>The host.</value>
        public string Host { get; set; }

        /// <summary>
        /// TCP port number to connect to
        /// </summary>
        /// <value>The port.</value>
        public int Port { get; set; }

        /// <summary>
        /// Longest period of inactivity before we try to reconnect
        /// </summary>
        /// <value>The max inactivity.</value>
        /// <remarks>Defaults to 47 seconds, since most services will send a
        /// keepalive every 45 seconds if there's no other messaging, and at 47
        /// we've exhausted any possibility of network latency.</remarks>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(47);

        /// <summary>
        /// EndOfMessage character
        /// </summary>
        /// <value>The eom.</value>
        /// <remarks>The major delimiter between messages. Defaults to a
        /// carriage-return for the most common one-line-per-mesage, but for
        /// multi-line messages try 0 for the null character.</remarks>
        public byte EOM { get; set; } = 13;

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = Observable
                            .Create<Message>(observer => TaskPoolScheduler.Default
                            .Schedule(() => ReadLoop(observer)));

                return _pipeline;
            }
        }
        private IObservable<Message> _pipeline;

        private void ReadLoop(IObserver<Message> observer)
        {
            bool Retrying = false;

            while (true) // Level where retries happen
            {
                using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(Host, Port))
                using (NetworkStream stream = client.GetStream())
                {
                    stream.ReadTimeout = (int)ReadTimeout.TotalMilliseconds;

                    try
                    {
                        if (Retrying)
                        {
                            // Push a null byte to jog a NAT into recycling the
                            // connection, if a NAT or firewall exists.
                            stream.Write(new byte[] { 0 }, 0, 1);
                            Retrying = false;
                        }

                        StringBuilder payload = new StringBuilder();
                        byte[] buffer = new byte[1024];
                        while (stream.CanRead)
                        {
                            int read = stream.Read(buffer, 0, 1024);

                            for (int i = 0; i < read; i++)
                            {
                                if (buffer[i] != EOM)
                                    payload.Append((char)buffer[i]);
                                else
                                {
                                    Message msg = payload.ToString().DeserialiseOrBust();
                                    observer.OnNext(msg);
                                    payload = new StringBuilder();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "Error reading stream");
                    }

                    Retrying = true;
                }
            }
        }
    }
}
