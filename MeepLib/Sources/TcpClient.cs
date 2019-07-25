using System;
using System.Linq;
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

using MeepLib.Messages;

namespace MeepLib.Sources
{
    /// <summary>
    /// Connect and listen to a remote service with a simple TCP connection
    /// </summary>
    /// <remarks>Often used for realtime feeds of prices, scores, telemetry, and other "live" sources. These eschew the
    /// overhead of an application-layer protocol like HTTP, so we just convert what we receive into raw string
    /// messages*.
    /// 
    /// <para>* - if the inbound message can be deserialised into a Meep Message or known subclass, it will. Since we
    /// use a Pipe and avoid as much malloc as we can, it's the recommended way of connecting two instances of Meep.
    /// </para>
    ///
    /// <para>Each Message should be serialised without its DerivedFrom chain. If ancestor Messages are needed by the
    /// receiver, it should request them by their ID. The TcpClient module doesn't attempt to do this, you'd use a
    /// broker module elsewhere in the pipeline instead.</para>
    ///
    /// </remarks>
    public class TcpClient : AMessageModule
    {
        /// <summary>
        /// Host name, without scheme (eg: host.domain.com or IP address)
        /// </summary>
        /// <value>The host.</value>
        public DataSelector Host { get; set; }

        /// <summary>
        /// TCP port number to connect to
        /// </summary>
        /// <value>The port.</value>
        /// <remarks>Defaults to 7769 ('M' and 'E' on the ASCII table)</remarks>
        public DataSelector Port { get; set; } = "7769";

        /// <summary>
        /// Longest period of inactivity before we try to reconnect
        /// </summary>
        /// <value>The max inactivity.</value>
        /// <remarks>Defaults to 47 seconds, since most services should send a keepalive every 45 seconds if there's no
        /// other messaging, plus we want a few seconds for margin.</remarks>
        public TimeSpan ReadTimeout { get; set; } = TimeSpan.FromSeconds(47);

        /// <summary>
        /// EndOfMessage character
        /// </summary>
        /// <value>The eom.</value>
        /// <remarks>The major delimiter between messages. Defaults to a carriage-return for the most common
        /// one-line-per-mesage, but for multi-line messages try 0 for the null character.
        ///
        /// <para>It would be nice to support strings as delimiters, which we should be able to do with an overload of
        /// ReadOnlySequence.PositionOf(), perhaps in the future.</para></remarks>
        public Byte EOM { get; set; } = 13;

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_pipeline == null)
                    _pipeline = Observable
                            .Create<Message>(observer => TaskPoolScheduler.Default
                            .Schedule(() => ReadMessages(observer)));

                return _pipeline;
            }
        }
        private IObservable<Message> _pipeline;

        private void ReadMessages(IObserver<Message> observer)
        {
            var context = new MessageContext(null, this);
            IPAddress ip = null;

            if (Host is null)
            {
                string hostName = Dns.GetHostName();
                ip = Dns.GetHostEntry(hostName).AddressList.First();
            }
            else
            {
                string dsHost = Host.SelectString(context);
                if (String.IsNullOrWhiteSpace(dsHost))
                    throw new ArgumentException("Host is empty or null");
                if (!IPAddress.TryParse(dsHost, out ip))
                    throw new ArgumentException(String.Format("Invalid Host IP address: {0}", dsHost));
            }


            var dsPort = Port.TrySelectLong(context);            
            if (!dsPort.Parsed)
                throw new ArgumentException("Invalid port number");

            bool Retrying = false;

            while (true) // Level where retries happen
            {
                using (var socket = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    try
                    {
                        //socket.Bind(new IPEndPoint(ip, (int)dsPort.Value));
                        //socket.Listen(120);
                        socket.Connect(new IPEndPoint(ip, (int)dsPort.Value));

                        if (Retrying)
                        {
                            // Push a null byte to jog a NAT into recycling the
                            // connection, if a NAT or firewall exists.
                            socket.SendAsync(new byte[] { 0 }, SocketFlags.None);
                            Retrying = false;
                        }

                        var pipe = new Pipe();

                        // Set up two tasks running concurrently: one reading from the socket
                        // and filling a Pipe, and the second pulling whole messages from that
                        // Pipe and feeding them into the Meep pipeline.
                        Task writing = FillPipeAsync(socket, pipe.Writer);
                        Task reading = ReadPipeAsync(pipe.Reader, EOM, observer);

                        Task.WhenAll(reading, writing);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn(ex, "{0} thrown when reading TCP socket: {1}", ex.GetType().Name, ex.Message);
                        Retrying = true;
                    }
                }
            }
        }

        private async Task FillPipeAsync(Socket socket, PipeWriter writer)
        {
            const int minimumBufferSize = 512;

            while (true)
            {
                try
                {
                    // Allocate at least 512 bytes from the PipeWriter
                    Memory<byte> memory = writer.GetMemory(minimumBufferSize);

                    int bytesRead = await socket.ReceiveAsync(memory, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        System.Threading.Thread.Sleep(100);
                    }

                    // Tell the PipeWriter how much was read from the Socket
                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    logger.Warn(ex, "{0} thrown when filling pipe from {1}: {2}", ex.GetType().Name, socket.RemoteEndPoint.ToString(), ex.Message);
                    break;
                }

                // Make the data available to the PipeReader
                FlushResult result = await writer.FlushAsync();

                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Tell the PipeReader that there's no more data coming
            writer.Complete();
        }

        private async Task ReadPipeAsync(PipeReader reader, byte eom, IObserver<Message> observer)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();

                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition? position = null;

                do
                {
                    // Look for an EOM in the buffer
                    position = buffer.PositionOf(eom);

                    if (position != null)
                    {
                        var msg = DeserialiseOrBust(buffer.Slice(0, position.Value));
                        observer.OnNext(msg);

                        // Skip the line + the \n character (basically position)
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);

                // Tell the PipeReader how much of the buffer we have consumed
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete
            reader.Complete();
        }

        //ToDo: Move DeserialiseOrBust into Extensions.cs when Visual Studio decides to stop crashing when we load it.

        /// <summary>
        /// Try to deserialise a string to a Meep Message, otherwise return a suitable IStringMessage
        /// </summary>
        /// <returns></returns>
        /// <param name="data">Data.</param>
        /// <remarks>Intended for bridging pipelines across IPC or networks where we don't want to burden the user to
        /// write explicit syntax that it's going to carry Meep Messages, we just want Meep to listen and go "oh okay,
        /// it's a Message".</remarks>
        public Message DeserialiseOrBust(ReadOnlySequence<byte> data)
        {
            // Does it fit the format "#00[typename]:{json here}", where 00 is the int16 length of [typename]?
            // If so, it's the format we use to serialise messages for IPC
            if (data.Length > 5)
            {
                // First memory sequence should always have enough to contain the whole type name, but we haven't tested
                // this yet and would imply some short packet lengths.
                var beginning = data.First.Span;

                // Speculatively grab the type name's length first so we can finish the rest of the test in one go
                UInt16 typeNameLen = BitConverter.ToUInt16(beginning.Slice(1, 2));

                if (beginning[0].Equals('#') && beginning[3 + typeNameLen].Equals(':'))
                    try
                    {
                        ReadOnlySpan<byte> typeName = beginning.Slice(1, typeNameLen);
                        Type msgType = Type.GetType(typeName.ToString());
                        if (msgType is null)
                            msgType = typeof(Message);

                        ReadOnlySequence<byte> json = data.Slice(4 + typeNameLen);
                        Utf8JsonReader reader = new Utf8JsonReader(json, false, new JsonReaderState());
                        var msg = JsonSerializer.ReadValue<Message>(ref reader);

                        if (msg != null)
                            return msg;
                    }
                    catch (Exception ex)
                    {
                        // If it matched our serialisation format, we should assume it's unusual to fail
                        // deserialisation, but we aren't going to stop the pipe because we are expecting to consume
                        // noisy feeds.
                        logger.Warn(ex, "{0} thrown when deserialising message: {1}", ex.GetType().Name, ex.Message);
                    }
            }

            return new ByteSequenceMessage(null, data);
        }

        private Message Deserialise(ReadOnlySequence<byte> slice)
        {
            Utf8JsonReader reader = new Utf8JsonReader(slice, false, new JsonReaderState());
            return JsonSerializer.ReadValue<Message>(ref reader);            
        }

        [Obsolete("Switch to Fill/ReadPipeAsync methods to use System.IO.Pipelines")]
        private void ReadLoop(IObserver<Message> observer)
        {
            var context = new MessageContext(null, this);
            string dsHost = Host.SelectString(context);
            IPAddress ip = null;
            if (!IPAddress.TryParse(dsHost, out ip))
                throw new ArgumentException(String.Format("Invalid Host IP address: {0}", dsHost));
            var dsPort = Port.TrySelectLong(context);

            if (String.IsNullOrWhiteSpace(dsHost))
                throw new ArgumentException("Host is empty or null");

            if (!dsPort.Parsed)
                throw new ArgumentException("Invalid port number");

            bool Retrying = false;

            while (true) // Level where retries happen
            {
                using (System.Net.Sockets.TcpClient client = new System.Net.Sockets.TcpClient(dsHost, (int)dsPort.Value))
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
                                    //Message msg = payload.ToString().DeserialiseOrBust();
                                    //observer.OnNext(msg);
                                    //payload = new StringBuilder();
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
