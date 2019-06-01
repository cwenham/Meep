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

        /// <summary>
        /// How often to send KeepAlive messages
        /// </summary>
        /// <remarks>Defaults to 5 minutes.</remarks>
        public TimeSpan KeepAlive { get; set; } = TimeSpan.FromMinutes(5);

        public override IObservable<Message> Pipeline
        {
            get
            {
                if (_pipeline is null)
                {
                    _pipeline = from msg in UpstreamMessaging
                                let result = ShippingAndHandling(msg)
                                where result != null
                                select result;
                }

                return _pipeline;
            }
            protected set
            {
                _pipeline = value;
            }
        }
        private IObservable<Message> _pipeline;

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

            string command = (await Command.SelectStrings(context))?.FirstOrDefault();

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
        /// Create an SshClient based on the connection parameters set in attributes
        /// </summary>
        /// <returns></returns>
        private SshClient CreateClient(MessageContext context)
        {
            var connectionInfoTask = GetConnectionInfo(context);
            connectionInfoTask.Wait();
            if (connectionInfoTask.Result is null)
                throw new ArgumentException("Insufficient connection details");            

            return new SshClient(connectionInfoTask.Result);
        }

        private string ConnectionName(ConnectionInfo info)
        {
            return $"{info.Username}@{info.Host}:{info.Port}";
        }

        /// <summary>
        /// Dictionary of clients by ConnectioName
        /// </summary>
        private Dictionary<string, SshClient> _clients = new Dictionary<string, SshClient>();

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
