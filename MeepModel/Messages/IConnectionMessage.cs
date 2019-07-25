using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace MeepLib.Messages
{
    public interface IConnectionMessage
    {
        /// <summary>
        /// IP Address of the remote machine
        /// </summary>
        IPAddress RemoteAddress { get; }

        /// <summary>
        /// Local port number that the remote machine connected to
        /// </summary>
        int Port { get; }

        /// <summary>
        /// True if the socket is still live and connected
        /// </summary>
        /// <returns></returns>
        bool Connected { get; }

        /// <summary>
        /// Send data to the connection asynchronously
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        ValueTask<int> SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancelToken);

        /// <summary>
        /// Close the connection
        /// </summary>
        void Close();

        /// <summary>
        /// True if this connection lasts indefinitely, such as a chat channel. False for one-offs that close after
        /// a Send, such as an HTTP request
        /// </summary>
        bool Persistent { get; }
    }
}
