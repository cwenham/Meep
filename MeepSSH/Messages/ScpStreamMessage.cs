using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading.Tasks;

using MeepLib;
using MeepLib.Messages;

namespace MeepSSH.Messages
{
    public class ScpStreamMessage : StreamMessage
    {
        public ScpStreamMessage(Task<Stream> stream)
        {
            _streamTask = stream;
        }

        private Task<Stream> _streamTask;

        public string RemoteHost { get; set; }

        public string RemoteFile { get; set; }

        public async override Task<Stream> GetStream()
        {
            return await _streamTask;
        }
    }
}
