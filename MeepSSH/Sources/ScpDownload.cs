using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Renci.SshNet;

using MeepLib;
using MeepLib.Messages;
using MeepLib.MeepLang;

using MeepSSH.Messages;

namespace MeepSSH.Sources
{
    /// <summary>
    /// Download a file by SCP (Secure Copy)
    /// </summary>
    /// <remarks>Can either download to a local file if the File attribute is set, or to a Memory Stream if you intend
    /// to pipe it somewhere else downstream.</remarks>
    [MeepNamespace(ASSHModule.PluginNamespace)]
    public class ScpDownload : AScp
    {
        protected override async Task<Message> HandleFile(MessageContext context, ScpClient client)
        {
            string sfLocalFile = File != null ? await File.SelectString(context) : null;
            string sfRemote = Remote != null ? await Remote.SelectString(context) : null;

            try
            {
                if (String.IsNullOrWhiteSpace(sfLocalFile))
                {
                    // No local file specified, so we'll return a StreamMessage subclass with a MemoryStream in it                
                    return new ScpStreamMessage(Task.Run<Stream>(() => {
                        MemoryStream stream = new MemoryStream();
                        client.Download(sfRemote, stream);
                        return stream;
                    }))
                    {
                        DerivedFrom = context.msg
                    };
                }

                client.Download(sfRemote, new FileInfo(sfLocalFile));
                return new LocalisedResource
                {
                    DerivedFrom = context.msg,
                    Local = sfLocalFile,
                    URL = String.Format("scp:{0}/{1}", ConnectionName(client.ConnectionInfo), sfRemote)
                };

            }
            catch (Exception ex)
            {
                logger.Warn(ex, "{0} thrown trying to SCP download {1}: {2}", ex.GetType().Name, sfRemote, ex.Message);
                return null;
            }
        }
    }
}
