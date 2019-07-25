using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace MeepLib.Messages
{
    /// <summary>
    /// A TCP client connecting to us
    /// </summary>
    /// <remarks>Encapsulates a live socket, so if serialised and sent to another instance of Meep it'll only arrive
    /// with the metadata (RemoteAddress, Port, Created) and a null socket.</remarks>
    [DataContract]
    public class TcpConnection : Message, IConnectionMessage
    {
        public TcpClient Client { get; set; }

        [DataMember]
        public IPAddress RemoteAddress { get; set; }

        [DataMember]
        public int Port { get; set; }

        public bool Persistent => true;

        public bool Connected => Client.Connected;

        public void Close()
        {
            Client.Close();
        }

        public ValueTask<int> SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancelToken)
        {
            return Client.Client.SendAsync(data, SocketFlags.None, cancelToken);
        }
    }
}
