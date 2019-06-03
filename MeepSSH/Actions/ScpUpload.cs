using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using Renci.SshNet;

using MeepLib;
using MeepLib.Messages;
using MeepLib.MeepLang;

using MeepSSH.Messages;
using System.Threading.Tasks;

namespace MeepSSH.Actions
{
    [MeepNamespace(ASSHModule.PluginNamespace)]
    public class ScpUpload : AScp
    {
        protected override async Task<Message> HandleFile(MessageContext context, ScpClient client)
        {
            string sfLocalFile = File != null ? await File.SelectString(context) : null;
            string sfRemote = Remote != null ? await Remote.SelectString(context) : null;

            StreamMessage smsg = context.msg as StreamMessage;
            if (smsg != null)
            {
                client.Upload(await smsg.GetStream(), sfRemote);
            }
            else
            {
                if (!System.IO.File.Exists(sfLocalFile))
                {
                    logger.Warn("{0} does not exist and cannot be uploaded to {1}", sfLocalFile, ConnectionName(client.ConnectionInfo));
                    return null;
                }

                client.Upload(new FileInfo(sfLocalFile), sfRemote);
            }

            return context.msg;
        }
    }
}
