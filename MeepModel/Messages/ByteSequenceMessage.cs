using System;
using System.Buffers;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

using Newtonsoft.Json;
using System.Collections.Generic;

namespace MeepLib.Messages
{
    /// <summary>
    /// A ReadOnlySequence of bytes
    /// </summary>
    [DataContract]
    public class ByteSequenceMessage : Message, IStringMessage
    {
        public ByteSequenceMessage()
        { }

        public ByteSequenceMessage(Message ancestor, ReadOnlySequence<byte> sequence)
        {
            this.DerivedFrom = ancestor;
            this.Sequence = sequence;
        }

        [DataMember]
        public ReadOnlySequence<byte> Sequence { get; set; }

        /// <summary>
        /// String representation of the sequence
        /// </summary>
        public string Value
        {
            get
            {
                return this.ToString();
            }
        }

        /// <summary>
        /// Convert the byte sequence to a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (_asString is null)
            {
                // Checked around but it looks like there's no built-in method for converting ReadOnlySequences of bytes
                // to strings, so we'll use StringBuilder. Revisit this after core 3 is released to see if they put one
                // in BitConverter or Encodings.
                StringBuilder builder = new StringBuilder();
                foreach (var seq in Sequence)
                    builder.Append(Encoding.UTF8.GetString(seq.Span));
                _asString = builder.ToString();
            }

            return _asString;
        }
        private string _asString;
    }
}
