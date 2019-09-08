using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace MeepLib.Messages
{
    /// <summary>
    /// A System.IO stream
    /// </summary>
    [DataContract]
    public class StreamMessage : Message, IStreamMessage
    {
        public StreamMessage()
        {
            // Default parameterless constructor
        }

        public StreamMessage(Stream stream)
        {
            _stream = stream;
        }

        protected Stream _stream;

        /// <summary>
        /// Task that returns the stream
        /// </summary>
        /// <value>The stream.</value>
        /// <remarks>Intended to be overridden in subclasses where the non-exceptional behaviour is to wait on a task
        /// that eventually delivers a stream. HttpResponseMessage.Content.ReadAsStreamAsync() for example, returns
        /// what this would return: a task waiting the milliseconds before a remote host begins sending a stream. We
        /// expose it as a Task so we don't have to block for those milliseconds.
        /// 
        /// <para>If the stream is already available, then it may not be necessary to subclass unless you need to
        /// package it with some metadata like an origin URL.</para>
        /// </remarks>
        public virtual async Task<Stream> GetStream()
        {
            return await Task.Run<Stream>(() =>
            {
                return _stream;
            });
        }
    }
}
