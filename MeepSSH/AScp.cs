using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using System.Reactive.Linq;

using Renci.SshNet;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepSSH
{
    /// <summary>
    /// Abstract base class for Secure Copy (Scp) modules
    /// </summary>
    [MeepNamespace(ASSHModule.PluginNamespace)]
    public abstract class AScp : ASSHModule
    {
        /// <summary>
        /// File on the local filesystem (Supports Meep type prefixes)
        /// </summary>
        /// <remarks>If left empty, we'll return a StreamMessage with the downloaded file.</remarks>
        public DataSelector File { get; set; }

        /// <summary>
        /// File on the remote host (Supports Meep type prefixes)
        /// </summary>
        public DataSelector Remote { get; set; }

        public async override Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            ConnectionInfo info = await GetConnectionInfo(context);
            string connectionName = ConnectionName(info);
            ScpClient client = null;
            if (_clients.ContainsKey(connectionName))
                client = _clients[connectionName];
            else
            {
                client = new ScpClient(info);
                client.KeepAliveInterval = KeepAlive;
                _clients.TryAdd(connectionName, client);
            }

            try
            {
                if (!client.IsConnected)
                    client.Connect();
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when trying to connect to {1}: {2}", ex.GetType().Name, connectionName, ex.Message);
                return null;
            }

            return await HandleFile(context, client);
        }

        protected abstract Task<Message> HandleFile(MessageContext context, ScpClient client);

        protected ConcurrentDictionary<string, ScpClient> _clients = new ConcurrentDictionary<string, ScpClient>();

        public override void Dispose()
        {
            foreach (var client in _clients.Values)
            {
                client.Disconnect();
                client.Dispose();
            }
        }
    }
}
