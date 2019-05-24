using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reactive.Linq;

using NLog;
using SmartFormat;
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
                    _client = CreateClient();
                    _client.KeepAliveInterval = KeepAlive;

                    _pipeline = from msg in UpstreamMessaging
                                let result = ShippingAndHandling(msg)
                                where result != null
                                select result;

                    _client.Connect();
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
            string command = (await Command.SelectStrings(context))?.FirstOrDefault();

            if (String.IsNullOrWhiteSpace(command))
                return null;

            var commandRunner = _client.CreateCommand(command);
            commandRunner.CommandTimeout = this.Timeout;

            var result = await Task.Factory.FromAsync((callback, stateObject) => commandRunner.BeginExecute(callback, stateObject), commandRunner.EndExecute, null);

            return new StringMessage
            {
                DerivedFrom = msg,
                Value = result
            };
        }

        /// <summary>
        /// Create an SshClient based on the connection parameters set in attributes
        /// </summary>
        /// <returns></returns>
        private SshClient CreateClient()
        {
            MessageContext context = new MessageContext(null, this);
            var connectionInfoTask = GetConnectionInfo(context);
            connectionInfoTask.Wait();
            if (connectionInfoTask.Result is null)
                throw new ArgumentException("Insufficient connection details");

            return new SshClient(connectionInfoTask.Result);
        }

        private SshClient _client;

        /// <summary>
        /// Disconnect from the remote host and dispose of SshClient
        /// </summary>
        public override void Dispose()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client.Dispose();
            }
        }
    }
}
