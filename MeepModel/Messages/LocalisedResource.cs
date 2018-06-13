using System;

namespace MeepModel.Messages
{
    public class LocalisedResource : Message
    {
        /// <summary>
        /// URL of the remote file or resource
        /// </summary>
        /// <value>The URL.</value>
        public string URL { get; set; }

        /// <summary>
        /// Local filesystem path
        /// </summary>
        /// <value>The local.</value>
        public string Local { get; set; }
    }
}
