using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using Renci.SshNet;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepSSH.Sources
{
    /// <summary>
    /// Establish an SSH connection to a remote host and execute commands on it based on upstream messages
    /// </summary>
    [MeepNamespace(ASSHModule.PluginNamespace)]
    public class Shell : ASSHModule
    {
        /// <summary>
        /// The command to issue on each inbound message (Supports Meep type prefixes)
        /// </summary>
        /// <remarks>Shell can receive upstream messages, which it will assume are commands to execute on the remote
        /// host. The command will be allowed to finish and its output issued as a StringMessage downstream.</remarks>
        public DataSelector Command { get; set; }

        public async override Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            ConnectionInfo info = await GetConnectionInfo(context);
            string connectionName = ConnectionName(info);
            SshClient client = null;
            if (_clients.ContainsKey(connectionName))
                client = _clients[connectionName];
            else
            {
                client = new SshClient(info);
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

            string command = await Command.SelectStringAsync(context);

            if (String.IsNullOrWhiteSpace(command))
                return null;

            try
            {
                var commandRunner = client.CreateCommand(command);
                commandRunner.CommandTimeout = this.Timeout;

                var result = await Task.Factory.FromAsync((callback, stateObject) => commandRunner.BeginExecute(callback, stateObject), commandRunner.EndExecute, null);

                return new StringMessage
                {
                    DerivedFrom = msg,
                    Value = result
                };
            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown when sending command to {1}: {2}", ex.GetType().Name, connectionName, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Dictionary of clients by ConnectioName
        /// </summary>
        private ConcurrentDictionary<string, SshClient> _clients = new ConcurrentDictionary<string, SshClient>();

        /// <summary>
        /// Disconnect from the remote host and dispose of SshClient
        /// </summary>
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
