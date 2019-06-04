using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Renci.SshNet;

using MeepLib;
using MeepLib.Filters;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepSSH.Filters
{
    /// <summary>
    /// Test an SSH host for the ability to log in with the given credentials
    /// </summary>
    /// <remarks>For creating fallback channels and safeguarding pipelines. Could also be used as part of a simple
    /// Pen testing tool.
    /// 
    /// <para>While one could bundle this with a Timer and the Emit module's list of common names and passwords
    /// to create a wardialer, it does not attempt to hide what it's doing and you will probably just get busted and
    /// shut down by your ISP or network admin for being dumb. Be warned!</para></remarks>
    [MeepNamespace(ASSHModule.PluginNamespace)]
    public class Accessible : ASSHModule, IPolarisedFilter
    {
        /// <summary>
        /// Timeout for testing connections
        /// </summary>
        /// <remarks>Defaults to 500 milliseconds.</remarks>
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(500);

        public override async Task<Message> HandleMessage(Message msg)
        {
            MessageContext context = new MessageContext(msg, this);
            ConnectionInfo info = await GetConnectionInfo(context);
            info.Timeout = ConnectionTimeout;
            string connectionName = ConnectionName(info);

            return await Task.Run<Message>(() =>
            {
                try
                {
                    using (SshClient client = new SshClient(info))
                    {
                        client.Connect();
                        if (client.IsConnected)
                            return ThisPassedTheTest(msg);
                        else
                            return ThisFailedTheTest(msg);
                    }
                }
                catch (Exception)
                {
                    return ThisFailedTheTest(msg);
                }
            });
        }

        public bool BlockOnMatch { get; set; } = false;

        public Message ThisFailedTheTest(Message msg)
        {
            if (BlockOnMatch)
                return msg;
            return null;
        }

        public Message ThisPassedTheTest(Message msg)
        {
            if (!BlockOnMatch)
                return msg;
            return null;
        }
    }
}
