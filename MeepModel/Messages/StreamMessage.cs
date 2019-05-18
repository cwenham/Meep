using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// A System.IO stream
    /// </summary>
    [DataContract]
    public class StreamMessage : Message, IStreamMessage
    {
        public Task<Stream> Stream { get; set; }

        /// <summary>
        /// The stream
        /// </summary>
        /// <value>The stream.</value>
        public async Task<Stream> GetStream()
        {
            return await Task.Run<Stream>(() =>
            {
                return Stream;
            });
        }
    }
}
