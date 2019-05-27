using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reactive.Linq;

using Renci.SshNet;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;
using System.Threading.Tasks;

namespace MeepSSH.Actions
{
    [MeepNamespace(ASSHModule.PluginNamespace)]
    public class SCP : ASSHModule
    {
        /// <summary>
        /// File on the local filesystem to copy or be written to (Supports Meep type prefixes)
        /// </summary>
        public DataSelector File { get; set; }

        /// <summary>
        /// File on the remote host to copy or be written to (Supports Meep type prefixes)
        /// </summary>
        public DataSelector Remote { get; set; }


    }
}
