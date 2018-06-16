using System;
using System.IO;

namespace MeepModel.Messages
{
    public class FileChanged : Message
    {
        /// <summary>
        /// Type of file change
        /// </summary>
        /// <value>The type of the change.</value>
        public WatcherChangeTypes ChangeType { get; set; }

        /// <summary>
        /// When the file changed
        /// </summary>
        /// <value>The modified.</value>
        public DateTime Modified { get; set; }

        /// <summary>
        /// Full path to file that changed
        /// </summary>
        /// <value>The full path.</value>
        public string FullPath { get; set; }

        /// <summary>
        /// Size of the file, in bytes
        /// </summary>
        /// <value>The size.</value>
        public long Size { get; set; }
    }
}
