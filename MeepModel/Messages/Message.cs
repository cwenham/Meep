using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// A message, the smallest unit of data in a Meep pipeline
    /// </summary>
    /// <remarks>All communication between modules in a pipeline take the form
    /// of messages derived from this class.</remarks>
    [DataContract]
    public class Message : IDisposable
    {
        /// <summary>
        /// Message ID
        /// </summary>
        /// <value>The identifier.</value>
        [DataMember, Key]
        public Guid ID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Optional name of generating module or other unique reference
        /// </summary>
        /// <value>The name.</value>
        [DataMember, NotMapped]
        public virtual string Name
        {
            get 
            {
                return _name ?? this.GetType().Name;
            }
            set 
            {
                _name = value;
            }
        }

        private string _name;

        /// <summary>
        /// Message that this was derived from
        /// </summary>
        /// <value>The derived from identifier.</value>
        [DataMember, Index(IsUnique = false)]
        public Guid DerivedFromID { get; set; }

        /// <summary>
        /// Reference to original message, if available
        /// </summary>
        /// <value>The derived from.</value>
        [XmlIgnore, Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, NotMapped]
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
        [DataMember, Index(IsUnique = false)]
        public long CreatedTicks { get; set; } = new DateTime(System.Diagnostics.Stopwatch.GetTimestamp()).ToUniversalTime().Ticks;

        /// <summary>
        /// When the message was created, in UTC
        /// </summary>
        /// <value>The created.</value>
        [XmlIgnore, Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, NotMapped]
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
        [DataMember, NotMapped]
        public long Deadline { get; set; }

        [XmlIgnore, Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, NotMapped]
        public string AsJSON
        {
            get
            {
                return this.ToJSON();
            }
        }

        [XmlIgnore, Newtonsoft.Json.JsonIgnore, System.Text.Json.Serialization.JsonIgnore, NotMapped]
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

        public static async Task<Message> TryDeserialise(Stream stream)
        {
            try
            {
                TextReader reader = new StreamReader(stream);
                string serialised = await reader.ReadToEndAsync();
                return JsonConvert.DeserializeObject<Message>(serialised);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public virtual void Dispose()
        {
            // Override in descendant classes that have resources to be mopped up.
            // Dispose() should be called by EOLMessage() in the Meep host.
        }
    }
}
