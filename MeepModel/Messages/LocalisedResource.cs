using System;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

namespace MeepLib.Messages
{
    /// <summary>
    /// A remote file resource that has been localised (cached)
    /// </summary>
    [DataContract]
    public class LocalisedResource : StringMessage
    {
        /// <summary>
        /// URL of the remote file or resource
        /// </summary>
        /// <value>The URL.</value>
        [DataMember]
        public string URL { get; set; }

        /// <summary>
        /// Local filesystem path
        /// </summary>
        /// <value>The local.</value>
        [DataMember]
        public string Local { get; set; }
    }
}
