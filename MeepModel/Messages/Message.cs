using System;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MeepModel.Messages
{
    [DataContract]
    public class Message
    {
        public Message()
        {
        }

        /// <summary>
        /// Message ID
        /// </summary>
        /// <value>The identifier.</value>
        [DataMember]
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Message that this was derived from
        /// </summary>
        /// <value>The derived from identifier.</value>
        [DataMember]
        public Guid DerivedFromID { get; set; }

        /// <summary>
        /// Reference to original message, if available
        /// </summary>
        /// <value>The derived from.</value>
        [DataMember]
        public Message DerivedFrom
        {
            get => _DerivedFrom;
            set
            {
                _DerivedFrom = value;
                if (value != null)
                    DerivedFromID = value.ID;
            }
        }
        private Message _DerivedFrom;

        /// <summary>
        /// Creation timestamp in UTC, set with high-resolution timer if available on platform
        /// </summary>
        /// <value>The created ticks.</value>
        [DataMember]
        public long CreatedTicks { get; set; } = new DateTime(System.Diagnostics.Stopwatch.GetTimestamp()).ToUniversalTime().Ticks;

        /// <summary>
        /// When the message was created, in UTC
        /// </summary>
        /// <value>The created.</value>
        public DateTime Created
        {
            get
            {
                if (!_created.HasValue)
                    _created = new DateTime(CreatedTicks);

                return _created.Value;
            }
        }
        private DateTime? _created;

        /// <summary>
        /// Deadline for processing the message, in ticks
        /// </summary>
        /// <value>The deadline.</value>
        /// <remarks>&lt;= 0 for no deadline.
        /// </remarks>
        [DataMember]
        public long Deadline { get; set; }

        /// <summary>
        /// Message payload
        /// </summary>
        /// <value>The value.</value>
        [DataMember]
        public object Value { get; set; }

        public override string ToString()
        {
            return Value?.ToString();
        }

        public int ToInt()
        {
            try
            {
                return (int)Value;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        [JsonIgnore, XmlIgnore]
        public string AsJSON
        {
            get
            {
                return this.ToJSON();
            }
        }

        [JsonIgnore, XmlIgnore]
        public string AsXML
        {
            get
            {
                return this.ToXML();
            }
        }

        /// <summary>
        /// Key used for caching in key=value stores
        /// </summary>
        /// <returns>The key.</returns>
        public virtual string GetKey()
        {
            return ToString()?.ToSHA256();
        }
    }
}
