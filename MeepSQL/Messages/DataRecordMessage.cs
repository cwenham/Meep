using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using MeepLib.Messages;

namespace MeepSQL.Messages
{
    [DataContract]
    public class DataRecordMessage : Message
    {
        /// <summary>
        /// Original DataRecord
        /// </summary>
        /// <value>The record.</value>
        public IDictionary<String, object> Record { get; set; }
    }
}
