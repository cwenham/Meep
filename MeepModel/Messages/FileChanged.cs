using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Threading.Tasks;

namespace MeepLib.Messages
{
    [DataContract]
    public class FileChanged : Message, IStreamMessage
    {
        /// <summary>
        /// Type of file change
        /// </summary>
        /// <value>The type of the change.</value>
        [DataMember, Index(IsUnique = false), JsonConverter(typeof(StringEnumConverter))]
        public WatcherChangeTypes ChangeType { get; set; }

        /// <summary>
        /// When the file changed
        /// </summary>
        /// <value>The modified.</value>
        [DataMember, Index(IsUnique = false)]
        public DateTime Modified { get; set; }

        /// <summary>
        /// Full path to file that changed
        /// </summary>
        /// <value>The full path.</value>
        [DataMember]
        public string FullPath { get; set; }

        /// <summary>
        /// Size of the file, in bytes
        /// </summary>
        /// <value>The size.</value>
        [DataMember, Index(IsUnique = false)]
        public long Size { get; set; }

        public Task<Stream> GetStream()
        {
            try
            {
                return Task.Run<Stream>(() => File.OpenRead(FullPath));
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
