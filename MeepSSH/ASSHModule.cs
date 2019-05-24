using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using NLog;
using SmartFormat;
using SshNet;
using Renci.SshNet;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

namespace MeepSSH
{
    public class ASSHModule : AMessageModule
    {
        public const string PluginNamespace = "http://meep.example.com/MeepSSH/V1";

        /// <summary>
        /// Host to connect to (Supports Meep type prefixes)
        /// </summary>
        public DataSelector Host { get; set; }

        /// <summary>
        /// Port number, if not the default (Supports Meep type prefixes)
        /// </summary>
        public DataSelector Port { get; set; }

        /// <summary>
        /// User to login as (Supports Meep type prefixes)
        /// </summary>
        public DataSelector User { get; set; }

        /// <summary>
        /// Password for the user (Supports Meep type prefixes)
        /// </summary>
        public DataSelector Pass { get; set; }

        /// <summary>
        /// File path or base64 encoded private key
        /// </summary>
        public DataSelector PrivateKey { get; set; }

        /// <summary>
        /// Passphrase protecting the key file (on disk, or base64 encoded forms)
        /// </summary>
        public DataSelector Passphrase { get; set; }

        /// <summary>
        /// Get Password or Private Key authentication method, according to attributes
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected async Task<AuthenticationMethod> GetAuthMethod(string username, MessageContext context)
        {
            if (PrivateKey != null)
            {
                var key = (await PrivateKey.SelectStrings(context)).FirstOrDefault();
                if (String.IsNullOrWhiteSpace(key))
                    return null;

                var passphrase = (await Passphrase.SelectStrings(context))?.FirstOrDefault();

                var keySmell = key.SmellsLike();
                if (keySmell == DataScent.UnixPath || keySmell == DataScent.WinPath)
                {
                    PrivateKeyFile file = new PrivateKeyFile(key,passphrase);
                    return new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile[] { file });
                }

                MemoryStream keyStream = new MemoryStream(Convert.FromBase64String(key));
                PrivateKeyFile keyFile = new PrivateKeyFile(keyStream, passphrase);
                return new PrivateKeyAuthenticationMethod(username, new PrivateKeyFile[] { keyFile });
            }

            if (Pass != null)
            {
                var password = (await Pass.SelectStrings(context)).FirstOrDefault();
                if (String.IsNullOrWhiteSpace(password))
                    return null;

                if (String.IsNullOrWhiteSpace(password))
                    return null;

                return new PasswordAuthenticationMethod(username, password);
            }

            return null;
        }

        protected int GetPort(MessageContext context)
        {
            if (Port is null)
                return 22;

            var portTask = Port.SelectStrings(context);
            portTask.Wait();

            string strPort = portTask.Result?.FirstOrDefault();
            if (String.IsNullOrWhiteSpace(strPort))
                return 22;

            if (int.TryParse(strPort, out int intPort))
                return intPort;
            else
                return 22;
        }

        protected async Task<ConnectionInfo> GetConnectionInfo(MessageContext context)
        {
            var host = (await Host.SelectStrings(context))?.FirstOrDefault();
            if (String.IsNullOrWhiteSpace(host))
                return null;

            var username = (await User.SelectStrings(context))?.FirstOrDefault();
            if (String.IsNullOrWhiteSpace(username))
                return null;

            var authMethod = await GetAuthMethod(username, context);
            if (authMethod is null)
                return null;

            return new ConnectionInfo(host, username, new AuthenticationMethod[] { authMethod });
        }
    }

    /// <summary>
    /// A proxy to SSHNet's shell that recieves commands from an upstream pipeline
    /// </summary>
    internal class MeepSSHProxy
    {
        public MeepSSHProxy(IObservable<Message> upstream)
        {
            Upstream = upstream;
        }

        /// <summary>
        /// Upstream messages are treated as commands to issue to the shell
        /// </summary>
        private IObservable<Message> Upstream;
    }
}
